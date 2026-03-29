using System.Collections.Generic;
using UIFramework.Models;
using UIFramework.Transform;

namespace TimeWindowTweaks.UIPatches
{
    public sealed class TimeWindowTransformer : UIPanelMetadataTransformer
    {
        /// <summary>
        /// Changes tooltip on the "Sleep" button in the Time Window
        /// </summary>
        protected override ICollection<UIPanelElement>? Visit(Button element, UIPanelElement[] ancestors) {
            if (element is { Text: "Sleep", ToolTip.Title: "Sleep" }) {
                return [
                    element with {
                        ToolTip = element.ToolTip with {
                            Message = "Let time pass until the next scheduled interchange service."
                        }
                    }
                ];
            }

            return base.Visit(element, ancestors);
        }
    }
}
