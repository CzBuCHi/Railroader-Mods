using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using AssetPack.Runtime;
using Audio;
using Model;
using Model.Database;
using Model.Definition.Data;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tweaks;

internal sealed class WhistlePicker : MonoBehaviour
{
    private static WhistlePicker? view;
    private Window window = null!;
    private UIBuilderAssets assets = null!;
    private UIPanel? panel;
    private Car car = null!;
    private string selected = string.Empty;
    private string selectedName = string.Empty;
    private string previewStatus = "Select a whistle to hear a 1-second preview.";
    private IConfigurableElement? installButton;
    private readonly Dictionary<string, Toggle> choices = new Dictionary<string, Toggle>();
    private CancellationTokenSource? loading;
    private LoadedAssetReference<AudioClip>? clip;
    private GameObject? previewObject;
    private WhistlePlayer? player;
    private WhistleProfile? profile;
    private float releaseAt;
    private float stopAt;
    private int request;

    internal static void Open(Car locomotive)
    {
        if (locomotive == null || !UI.CarCustomizeWindow.CarCustomizeWindow.CanCustomize(locomotive, out _)) return;
        try
        {
            if (view == null)
            {
                var creator = Object.FindObjectOfType<ProgrammaticWindowCreator>();
                if (creator == null || creator.windowPrefab == null) return;
                var native = Object.Instantiate(creator.windowPrefab, creator.transform, false);
                native.name = TweaksPlugin.Identifier + ".WhistlePicker";
                view = native.gameObject.AddComponent<WhistlePicker>();
                view.window = native; view.assets = creator.builderAssets;
                native.Title = "Whistles";
                native.SetInitialPositionSize(native.name, new Vector2(540, 520), Window.Position.CenterRight,
                    Window.Sizing.Resizable(new Vector2Int(440, 380)));
                if (native.contentRectTransform.GetComponent<RectMask2D>() == null)
                    native.contentRectTransform.gameObject.AddComponent<RectMask2D>();
                native.CloseWindow();
            }
            view.StopPreview();
            view.car = locomotive;
            view.selected = RepairSounds.CurrentWhistle(locomotive);
            view.selectedName = RepairSounds.Whistles(locomotive).FirstOrDefault(item => item.Id == view.selected).Name ?? "Default";
            view.previewStatus = "Select a whistle to hear a 1-second preview.";
            view.Populate();
            view.window.ShowWindow();
        }
        catch (Exception error) { Toast.Present("Could not open whistles: " + error.Message); }
    }

    private void Populate()
    {
        panel?.Dispose();
        choices.Clear();
        panel = UIPanel.Create(window.contentRectTransform, assets, builder => {
            builder.AddField("Preview", () => previewStatus, UIPanelBuilder.Frequency.Periodic);
            builder.AddField("Installation", () => {
                if (car == null || !RepairSounds.Ready) return "Unavailable";
                if (SoundInstallation.VisiblePending(car)) return SoundInstallation.Status(car);
                if (!RepairSounds.CanChange(car)) return "Requires a repair track";
                return TweaksPlugin.Settings.ManualSoundInstallationDelay ? "5 game minutes on this repair track" : "Immediate change on this repair track";
            }, UIPanelBuilder.Frequency.Periodic);
            var scroll = builder.VScrollView(list => {
                foreach (var choice in RepairSounds.Whistles(car).OrderBy(item => item.Name))
                {
                    var id = choice.Id; var name = choice.Name;
                    list.HStack(row => {
                        var rect = row.AddToggle(() => selected == id, value => {
                            if (!value) { choices[id].SetIsOnWithoutNotify(selected == id); return; }
                            selected = id; selectedName = name;
                            foreach (var item in choices) item.Value.SetIsOnWithoutNotify(item.Key == id);
                            Preview(id);
                        });
                        choices[id] = rect.GetComponent<Toggle>();
                        row.AddLabel(name);
                    }, 8);
                }
            });
            var layout = scroll.GetComponent<LayoutElement>();
            layout.minHeight = 160f; layout.preferredHeight = 320f;
            builder.ButtonStrip(strip => {
                installButton = strip.AddButton("OK", Install);
                strip.AddButton("Cancel", window.CloseWindow);
            });
        });
    }

    private void Install()
    {
        if (car == null || !RepairSounds.CanChange(car) || SoundInstallation.VisiblePending(car)) return;
        var changed = selected != RepairSounds.CurrentWhistle(car);
        SoundInstallation.QueueWhistle(car, selected);
        window.CloseWindow();
        Toast.Present(!changed ? "Keeping the current whistle." :
            TweaksPlugin.Settings.ManualSoundInstallationDelay ? "Whistle installation started (5 game minutes)." : "Whistle changed.");
    }

    private async void Preview(string id)
    {
        StopPreview();
        var version = request;
        var cancellation = new CancellationTokenSource();
        loading = cancellation;
        previewStatus = "Loading " + selectedName + "...";
        LoadedAssetReference<AudioClip>? loaded = null;
        try
        {
            var store = TrainController.Shared.PrefabStore;
            var definition = store.DefinitionForIdentifier<WhistleDefinition>(id, out _);
            if (definition.Audio.IsEmpty) { previewStatus = "No preview audio."; return; }
            var reference = store.ResolveAssetReference(id, definition.Audio);
            loaded = await store.LoadAssetAsync<AudioClip>(reference, cancellation.Token);
            if (this == null || version != request || !isActiveAndEnabled) return;
            var template = car.GetComponentInChildren<WhistlePlayer>(true)
                ?? Resources.FindObjectsOfTypeAll<WhistlePlayer>().FirstOrDefault(item => item != null && item.profile != null);
            if (template == null || template.profile == null) { previewStatus = "Whistle audio profile unavailable."; return; }
            clip = loaded; loaded = null;
            profile = Object.Instantiate(template.profile);
            profile.audioClip = clip.Asset;
            previewObject = new GameObject(TweaksPlugin.Identifier + ".WhistlePreview");
            previewObject.SetActive(false);
            previewObject.transform.position = Camera.main != null ? Camera.main.transform.position : car.transform.position;
            player = previewObject.AddComponent<WhistlePlayer>();
            player.profile = profile; player.parameter = 1f;
            previewObject.SetActive(true);
            releaseAt = Time.unscaledTime + 0.8f; stopAt = releaseAt + 0.2f;
            previewStatus = "Playing " + selectedName;
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (this != null && version == request)
            {
                StopPreview(); previewStatus = "Preview failed: " + error.Message;
                Debug.LogWarning("[Tweaks] Whistle preview failed: " + error);
            }
        }
        finally
        {
            loaded?.Dispose();
            if (loading == cancellation) loading = null;
            cancellation.Dispose();
        }
    }

    private void Update()
    {
        if (car == null || !RepairSounds.Ready) { window.CloseWindow(); return; }
        installButton?.Disable(!RepairSounds.CanChange(car) || SoundInstallation.VisiblePending(car));
        if (player == null) return;
        if (Camera.main != null) previewObject!.transform.position = Camera.main.transform.position;
        if (Time.unscaledTime >= releaseAt) player.parameter = 0f;
        if (Time.unscaledTime >= stopAt) StopPreview();
    }

    private void StopPreview()
    {
        request++;
        loading?.Cancel(); loading = null;
        if (previewObject != null) { previewObject.SetActive(false); Object.Destroy(previewObject); }
        previewObject = null; player = null;
        if (profile != null) Object.Destroy(profile);
        profile = null;
        clip?.Dispose(); clip = null;
        previewStatus = "Select a whistle to hear a 1-second preview.";
    }
    internal static void DestroyWindow()
    {
        if (view != null) { view.StopPreview(); Object.Destroy(view.gameObject); }
        view = null;
    }
    private void OnDisable() { StopPreview(); panel?.Dispose(); panel = null; }
    private void OnDestroy() { StopPreview(); panel?.Dispose(); panel = null; if (view == this) view = null; }
}
