using System;
using System.Collections.Generic;
using System.Linq;
using Game.Events;
using Game.Progression;
using JetBrains.Annotations;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.UI;

[PublicAPI]
public sealed class MilestonesWindow : MonoBehaviour, IProgrammaticWindow
{
    public        UIBuilderAssets  BuilderAssets    { get; set; } = null!;
    public        string           WindowIdentifier { get; }      = "MilestonesWindow";
    public        Vector2Int       DefaultSize      { get; }      = new(800, 600);
    public        Window.Position  DefaultPosition  { get; }      = Window.Position.Center;
    public        Window.Sizing    Sizing           { get; }      = Window.Sizing.Fixed(new(800, 600));

    public static MilestonesWindow Shared           => WindowManager.Shared!.GetWindow<MilestonesWindow>()!;

    private Window   _Window = null!;
    private UIPanel? _Panel;

    public void Awake() => _Window = GetComponent<Window>()!;

    public void Show() {
        Populate();
        _Window.ShowWindow();
    }

    public static void Toggle() {
        if (Shared._Window.IsShown) {
            Shared._Window.CloseWindow();
            EditorState.Reset();
        } else {
            Shared.Show();
        }
    }

    public void OnDisable() {
        _Panel?.Dispose();
        _Panel = null;
    }

    private void Populate() {
        _Window.Title = "Map Editor - Milestones Manager";
        _Panel?.Dispose();
        _Panel = UIPanel.Create(_Window.contentRectTransform!, BuilderAssets, Build);
    }

    private readonly UIState<string?> _SelectedItem = new(null);

    private void Build(UIPanelBuilder builder) {
        var shared = Progression.Shared;
        if (shared == null) {
            builder.AddLabel("Milestones not available. Please quit and reload this save in company mode.");
            return;
        }

        builder.RebuildOnEvent<ProgressionStateDidChange>();

        var prerequisites = shared.Sections!
                                  .SelectMany(o => o.prerequisiteSections, (o, p) => (o, p))
                                  .GroupBy(t => t.p, t => t.o)
                                  .ToDictionary(o => o.Key, o => o.ToArray());

        var sections = shared.Sections!.OrderBy(SectionIndexForSection)
                             .Select(section => {
                                 var text = section.Unlocked || section.Available ? section.displayName! : "<i>" + section.displayName + "</i>";
                                 return new UIPanelBuilder.ListItem<Section>(section.identifier!, section, SectionNameForSection(section), text);
                             }).ToList();

        builder.AddListDetail(sections, _SelectedItem, BuildDetail(prerequisites));

        builder.AddExpandingVerticalSpacer();
    }

    private static void BuildDetail(UIPanelBuilder builder, Section? section, Dictionary<Section, Section[]> prerequisites) {
        if (section == null) {
            builder.AddLabel("No milestone selected.");
            builder.AddExpandingVerticalSpacer();
            return;
        }

        if (section.Available) {
            var length = section.deliveryPhases!.Length;
            builder.AddTitle(section.displayName!, $"{section.FulfilledCount}/{length} Phases Complete");
            builder.AddLabel(section.description!);
            builder.AddButton("Advance", () => Advance(section));
            if (length > 1) {
                builder.AddButton("Advance one phase", () => Progression.Shared!.Advance(section));
            }
        } else if (section.Unlocked) {
            builder.AddTitle(section.displayName!, "Completed!");
            builder.AddLabel(section.description!);
            builder.AddButton("Revert", () => Revert(section, prerequisites));
        } else {
            builder.AddTitle(section.displayName!, "Not yet available.");
            builder.AddLabel(section.description!);
            builder.AddSection("Prerequisites", prerequisite => {
                foreach (var prerequisiteSection in section.prerequisiteSections!) {
                    prerequisite.AddLabel(prerequisiteSection.displayName + " - " + (prerequisiteSection.Unlocked ? "Completed" : "Not Complete"));
                }
            });
            builder.AddButton("Advance Prerequisites", () => AdvancePrerequisites(section));
        }

        builder.AddExpandingVerticalSpacer();
    }

    private static Action<UIPanelBuilder, Section?> BuildDetail(Dictionary<Section, Section[]> prerequisites) => (builder, section) => BuildDetail(builder, section, prerequisites);

    private static void AdvancePrerequisites(Section section) {
        foreach (var prerequisite in section.prerequisiteSections!.Where(o => !o.Unlocked)) {
            AdvancePrerequisites(prerequisite);
            Advance(prerequisite);
        }
    }

    private static void Advance(Section section) {
        foreach (var _ in section.deliveryPhases!) {
            Progression.Shared!.Advance(section);
        }
    }

    private static void Revert(Section section, Dictionary<Section, Section[]> prerequisites) {
        if (prerequisites.TryGetValue(section, out var sectionPrerequisites) && sectionPrerequisites != null) {
            foreach (var prerequisite in sectionPrerequisites.Where(o => o.Unlocked)) {
                Revert(prerequisite, prerequisites);
            }
        }

        global::UI.Console.Console.shared.AddLine($"Revert: {section.displayName}");
        foreach (var _ in section.deliveryPhases!) {
            Progression.Shared!.Revert(section);
        }
    }

    private static int SectionIndexForSection(Section section) =>
        section.Unlocked       ? 3 :
        !section.Available     ? 2 :
        section.PaidCount <= 0 ? 1 : 0;

    private static string SectionNameForSection(Section section) =>
        section.Unlocked       ? "Complete" :
        !section.Available     ? "Not Yet Available" :
        section.PaidCount <= 0 ? "Available" :
                                 "In Progress";
}
