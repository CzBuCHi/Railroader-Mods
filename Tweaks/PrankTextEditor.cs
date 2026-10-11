using System;
using System.IO;
using System.Text;
using TMPro;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tweaks;

internal sealed class PrankTextEditor : MonoBehaviour
{
    private static PrankTextEditor? view;
    private Window window = null!;
    private UIBuilderAssets assets = null!;
    private UIPanel? panel;
    private string draft = string.Empty;
    internal static string TextPath => Path.Combine(Path.GetDirectoryName(typeof(PrankLettering).Assembly.Location)!, PrankLettering.FileName);
    internal static void Open()
    {
        try
        {
            if (view == null)
            {
                var creator = Object.FindObjectOfType<ProgrammaticWindowCreator>();
                if (creator == null || creator.windowPrefab == null) { Toast.Present("Open a game before editing prank texts."); return; }
                var native = Object.Instantiate(creator.windowPrefab, creator.transform, false);
                native.name = TweaksPlugin.Identifier + ".PrankTexts";
                view = native.gameObject.AddComponent<PrankTextEditor>();
                view.window = native; view.assets = creator.builderAssets;
                native.Title = "Prank texts";
                native.SetInitialPositionSize(native.name, new Vector2(640, 460), Window.Position.CenterRight,
                    Window.Sizing.Resizable(new Vector2Int(560, 360)));
                if (native.contentRectTransform.GetComponent<RectMask2D>() == null) native.contentRectTransform.gameObject.AddComponent<RectMask2D>();
                native.CloseWindow();
            }
            if (view.window.IsShown) return;
            view.draft = File.Exists(TextPath) ? File.ReadAllText(TextPath, Encoding.UTF8) : string.Empty;
            view.Populate(); view.window.ShowWindow();
        }
        catch (Exception error) { Toast.Present("Could not open prank texts: " + error.Message); }
    }
    private void Populate()
    {
        panel?.Dispose();
        panel = UIPanel.Create(window.contentRectTransform, assets, builder => {
            var hint = builder.AddLabel("One line = one choice. Enter adds a choice; type \\n for a line break on the vehicle.");
            hint.GetComponentInChildren<TMP_Text>().parseCtrlCharacters = false;
            builder.AddLabel("Maximum 100 characters per sign. Blank lines are ignored.");
            var rect = builder.AddMultilineTextEditor(draft, "One sign per line...", text => draft = text, text => draft = text);
            var input = rect.GetComponentInChildren<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.characterLimit = 0;
            input.textComponent.richText = false;
            input.textComponent.parseCtrlCharacters = false;
            var layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 200f; layout.preferredHeight = 280f; layout.flexibleHeight = 1f;
            builder.ButtonStrip(strip => {
                strip.AddButton("Save", Save);
                strip.AddButton("Close", window.CloseWindow);
            });
        });
    }
    private void Save()
    {
        try
        {
            var text = draft.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
            if (File.Exists(TextPath)) File.Copy(TextPath, TextPath + ".bak", true);
            File.WriteAllText(TextPath, text, new UTF8Encoding(true));
            var count = PrankTextList.Parse(text.Split(new[] { "\r\n" }, StringSplitOptions.None)).Length;
            Toast.Present("Saved " + count + " prank texts.");
        }
        catch (Exception error) { Toast.Present("Could not save prank texts: " + error.Message); }
    }
    internal static void DestroyWindow()
    {
        if (view != null) Object.Destroy(view.gameObject);
        view = null;
    }
    private void OnDisable() { panel?.Dispose(); panel = null; }
    private void OnDestroy() { panel?.Dispose(); panel = null; if (view == this) view = null; }
}
