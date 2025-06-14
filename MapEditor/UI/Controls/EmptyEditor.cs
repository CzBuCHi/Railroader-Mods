using Helpers;
using MapEditor.Extensions;
using MapEditor.Utility;
using StrangeCustoms.Tracks;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace MapEditor.UI.Controls;

public static class EmptyEditor
{
    private static Camera _MainCamera = null!;

    public static void Build(UIPanelBuilder builder) {
        if (!MainCameraHelper.TryGetIfNeeded(ref _MainCamera)) {
            return;
        }

        if (!WorldTransformer.TryGetShared(out var worldTransformer) || worldTransformer == null) {
            builder.AddLabel("Error: Cannot find World");
            return;
        }

        builder.ButtonStrip(strip => {
            strip.AddButton("Scene Viewer", SceneWindow.Toggle);
            strip.AddButton("Place object", PlaceObject);
        });

        return;

        void PlaceObject() {
            Toast.Present("Click on ground to continue ...");
            UnityHelpers.CallOnceOnMouseButton(0, () => {
                if (!UnityHelpers.RayPointFromMouse(_MainCamera, out var point)) {
                    return;
                }

                SceneWindow.SelectTemplate(template => {
                    template.SetActive(false);

                    var go = Object.Instantiate(template)!;
                    template.SetActive(true);

                    go.transform.SetParent(template.transform.parent, true);
                    go.transform.position = point;
                    go.name = IdGenerators.Scenery.Next();
                    go.SetActive(true);

                    var sceneryAssetInstance = go.GetComponent<SceneryAssetInstance>()!;
                    EditorState.ReplaceSelection(sceneryAssetInstance);
                    MapEditorPlugin.PatchEditor!.AddOrUpdateScenery(sceneryAssetInstance.name!, new SerializedScenery(sceneryAssetInstance));
                });
            });
        }
    }
}
