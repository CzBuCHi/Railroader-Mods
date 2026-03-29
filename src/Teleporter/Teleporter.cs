using System;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Serilog;
using Teleporter.UI;
using UIFramework.Patches;
using UIFramework.Tools;
using UnityModManagerNet;

namespace Teleporter
{
    public sealed class Teleporter
    {
        private readonly UnityModManager.ModEntry _ModEntry;

        public Teleporter(UnityModManager.ModEntry modEntry) {
            _ModEntry = modEntry;

            ProgrammaticWindowCreatorPatches.RegisterWindow<TeleporterWindow>();

            Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));

            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
        }

        public Settings Settings { get; }

        public void Dispose() {
            Log.Information("Teleporter: Dispose");
            Messenger.Default.Unregister(this);
        }

        private void OnMapDidLoad(MapDidLoadEvent @event) {
            Log.Information("Teleporter: OnMapDidLoad");
            TopRightAreaHelper.AddButton(typeof(Main).Assembly, "Teleporter.icon.png", "Teleporter", 9, TeleporterWindow.Toggle);

            if (Settings.AutoOpenTeleporterWindow) {
                TeleporterWindow.Toggle();
            }
        }

        public void SaveSettings() {
            Settings.Save(_ModEntry);
        }
    }
}
