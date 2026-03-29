using System.Collections.Generic;
using UI.Builder;
using UIFramework.Models;

namespace UIFramework.Transform
{
    /// <summary>
    /// Generator that takes metadata and builds the actual UI using the builder. This is where the metadata is translated into actual UI elements.
    /// </summary>
    public static class UIPanelGenerator
    {
        public static void Build(UIPanelBuilder builder, UIPanelMetadata metadata) {
            if (metadata.Spacing != null) {
                builder.Spacing = metadata.Spacing.Value;
            }

            Build(builder, metadata.Elements);
        }

        private static void Build(UIPanelBuilder builder, ICollection<UIPanelElement> elements) {
            foreach (var element in elements) {
                Build(builder, element);
            }
        }

        private static void Build(UIPanelBuilder builder, UIPanelElement element) {
            switch (element) {
                case DynamicLabel dynamicLabel: {
                    var ui = builder.AddLabel(dynamicLabel.ValueClosure, dynamicLabel.Frequency);
                    if (dynamicLabel.HorizontalTextAlignment != null) {
                        ui.HorizontalTextAlignment(dynamicLabel.HorizontalTextAlignment.Value);
                    }

                    if (dynamicLabel.VerticalTextAlignment != null) {
                        ui.VerticalTextAlignment(dynamicLabel.VerticalTextAlignment.Value);
                    }

                    break;
                }

                case ExpandingVerticalSpacer:
                    builder.AddExpandingVerticalSpacer();
                    break;

                case Section section:
                    builder.AddSection(section.SectionTitle);
                    break;

                case Label label:
                    builder.AddLabel(label.Text);
                    break;

                case HStack hStack:
                    builder.HStack(hStackBuilder => Build(hStackBuilder, hStack.Elements), hStack.Spacing);
                    break;

                case Button button: {
                    var ui = builder.AddButton(button.Text, button.Action);
                    if (button.ToolTip != null) {
                        ui.Tooltip(button.ToolTip.Title, button.ToolTip.Message);
                    }

                    break;
                }

                default:
                    builder.AddLabel("NOT SUPPORTED: " + element.GetType().Name);
                    break;
            }
        }
    }
}
