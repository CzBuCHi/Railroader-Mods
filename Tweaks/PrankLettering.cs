using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Game.Messages;
using Game.State;
using Model;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using UnityEngine;

namespace Tweaks;

internal static class PrankLettering
{
    internal const string FileName = "PrankTexts.txt";
    private static bool reportedFailure;
    internal static bool Randomize(Car locomotive)
    {
        if (!TweaksPlugin.Settings.PrankLettering) return false;
        var target = locomotive;
        // Match the game's tender lookup; tank engines and diesels use their own lettering.
        if (locomotive is SteamLocomotive steam && steam.hasTender)
        {
            if (!steam.TryGetAdjacentCar(steam.EndToLogical(Car.End.R), out var tender) || tender.Archetype != CarArchetype.Tender) return false;
            target = tender;
        }
        if (!target.IsOwnedByPlayer || !target.Definition.Components.OfType<DecalComponent>().Any(decal => decal.Content == DecalContent.Lettering)) return false;
        try
        {
            var path = Path.Combine(Path.GetDirectoryName(typeof(PrankLettering).Assembly.Location)!, FileName);
            if (!File.Exists(path)) return false;
            var choices = PrankTextList.Parse(File.ReadAllLines(path, Encoding.UTF8));
            if (choices.Length == 0) return false;
            var current = target.KeyValueObject["lettering.basic"].StringValue;
            var index = RepairSounds.RandomOther(choices.Length, Array.IndexOf(choices, current));
            if (choices[index] == current) return false;
            var message = new PropertyChange(target.id, "lettering.basic", new StringPropertyValue(choices[index]));
            if (!StateManager.CheckAuthorizedToSendMessage(message)) return false;
            StateManager.ApplyLocal(message);
            return true;
        }
        catch (Exception error)
        {
            if (!reportedFailure) { Debug.LogWarning("[Tweaks] Could not load prank texts: " + error.Message); reportedFailure = true; }
            return false;
        }
    }
}
