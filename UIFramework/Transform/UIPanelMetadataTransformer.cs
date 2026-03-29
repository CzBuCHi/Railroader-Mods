using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UIFramework.Models;

namespace UIFramework.Transform
{
    /// <summary>
    /// Transformer that can be used to modify UIPanelMetadata. This is useful for things like adding new UI elements to existing UIs.
    /// </summary>
    [PublicAPI]
    public abstract class UIPanelMetadataTransformer
    {
        private ICollection<UIPanelElement>? Visit(UIPanelElement element, UIPanelElement[] ancestors) {
            return element switch {
                Button typedElement                  => Visit(typedElement, ancestors),
                DynamicLabel typedElement            => Visit(typedElement, ancestors),
                ExpandingVerticalSpacer typedElement => Visit(typedElement, ancestors),
                HStack typedElement                  => Visit(typedElement, ancestors),
                Label typedElement                   => Visit(typedElement, ancestors),
                Section typedElement                 => Visit(typedElement, ancestors),
                // Note: we intentionally don't have a default case here so that the compiler will warn us if we add new UIPanelElement types without adding corresponding Visit methods
                _ => throw new NotImplementedException($"No Visit implementation for {element.GetType().Name}")
            };
        }

        private TContainer? VisitContainer<TContainer>(TContainer container, ICollection<UIPanelElement> children, UIPanelElement[] ancestors, Func<ICollection<UIPanelElement>, TContainer> reconstruct)
            where TContainer : UIPanelElement {
            var newChildren = VisitChildren(container, children, ancestors);
            return ReferenceEquals(newChildren, children) ? null : reconstruct(newChildren);
        }

        private ICollection<UIPanelElement> VisitChildren(UIPanelElement parent, ICollection<UIPanelElement> children, UIPanelElement[] ancestors) {
            var updated = new List<UIPanelElement>();
            var changed = false;

            var ancestorsAndParent = ancestors.Append(parent).ToArray();

            foreach (var child in children) {
                var result = Visit(child, ancestorsAndParent);

                if (result == null) {
                    updated.Add(child);
                } else {
                    changed = true;
                    updated.AddRange(result);
                }
            }

            return changed ? updated : children;
        }

        // Per-element Visit methods (default: no change)
        protected virtual ICollection<UIPanelElement>? Visit(Button element, UIPanelElement[] ancestors)                  => null;
        protected virtual ICollection<UIPanelElement>? Visit(DynamicLabel element, UIPanelElement[] ancestors)            => null;
        protected virtual ICollection<UIPanelElement>? Visit(ExpandingVerticalSpacer element, UIPanelElement[] ancestors) => null;
        protected virtual ICollection<UIPanelElement>? Visit(Label element, UIPanelElement[] ancestors)                   => null;
        protected virtual ICollection<UIPanelElement>? Visit(Section element, UIPanelElement[] ancestors)                 => null;

        protected virtual ICollection<UIPanelElement>? Visit(HStack element, UIPanelElement[] ancestors) {
            var hStack = VisitContainer(element, element.Elements, ancestors, elements => element with { Elements = elements });
            return hStack != null ? [hStack] : null;
        }

        public virtual UIPanelMetadata Visit(UIPanelMetadata metadata) => VisitContainer(metadata, metadata.Elements, [], elements => metadata with { Elements = elements }) ?? metadata;
    }
}