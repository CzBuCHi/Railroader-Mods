using System;
using System.Collections.Generic;
using TMPro;
using UI.Builder;

namespace UIFramework.Models
{
    public abstract record UIPanelElement;

    public sealed record UIPanelMetadata(ICollection<UIPanelElement> Elements) : UIPanelElement
    {
        public float? Spacing { get; init; }
    }

    public sealed record Button(string Text, Action Action) : UIPanelElement
    {
        public ToolTip? ToolTip { get; init; }
    }

    public sealed record DynamicLabel(Func<string> ValueClosure, UIPanelBuilder.Frequency Frequency) : UIPanelElement
    {
        public HorizontalAlignmentOptions? HorizontalTextAlignment { get; init; }
        public VerticalAlignmentOptions?   VerticalTextAlignment   { get; init; }
    }

    public sealed record ExpandingVerticalSpacer : UIPanelElement;

    public sealed record HStack(params ICollection<UIPanelElement> Elements) : UIPanelElement
    {
        public float Spacing { get; init; } = 4f;
    }

    public sealed record Label(string Text) : UIPanelElement;

    public sealed record Section(string SectionTitle) : UIPanelElement;

    public sealed record ToolTip(string Title, string Message);
}