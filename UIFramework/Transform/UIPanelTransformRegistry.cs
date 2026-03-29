using System;
using System.Collections.Generic;
using UIFramework.Models;

namespace UIFramework.Transform
{
    /// <summary>
    /// Registry for UIPanelMetadataTransformers. This allows us to register transformers that will be applied to all metadata before it is used to build the UI.
    /// This is useful for things like adding new UI elements to existing UIs without needing to patch every single method that builds the UI.
    /// </summary>
    public static class UIPanelTransformRegistry
    {
        private static readonly List<Type> _Transformers = new();

        public static void RegisterTransformer<TTransformer>() where TTransformer : UIPanelMetadataTransformer, new() {
            _Transformers.Add(typeof(TTransformer));
        }

        public static void UnregisterTransformer<TTransformer>() where TTransformer : UIPanelMetadataTransformer {
            _Transformers.Remove(typeof(TTransformer));
        }

        public static UIPanelMetadata Transform(UIPanelMetadata metadata) {
            foreach (var transformer in _Transformers) {
                var instance = (UIPanelMetadataTransformer)Activator.CreateInstance(transformer)!;
                metadata = instance.Visit(metadata);
            }

            return metadata;
        }
    }
}
