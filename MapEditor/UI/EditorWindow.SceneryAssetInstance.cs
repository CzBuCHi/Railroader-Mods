using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using MapEditor.Behaviours;
using MapEditor.Extensions;
using StrangeCustoms.Tracks;
using UI.Builder;
using UnityEngine;

namespace MapEditor.UI;

public sealed partial class EditorWindow
{
    private List<string>? _Identifiers;

    private int _SceneryAssetInstanceIdentifierIndex;

    private void BuildSceneryAssetInstanceEditor(UIPanelBuilder builder, SceneryAssetInstance sceneryAssetInstance) {
        _Identifiers ??= FindObjectsByType<SceneryAssetInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)!
                         .Select(o => o.identifier).Distinct().OrderBy(o => o).ToList();

        builder.AddField("Id", builder.AddInputField(sceneryAssetInstance.name, _ => { })).Disable(true);
        builder.AddField("Position", builder.AddInputField(sceneryAssetInstance.transform.localPosition.ToString(), _ => { })).Disable(true);
        builder.AddField("Rotation", builder.AddInputField(sceneryAssetInstance.transform.localEulerAngles.ToString(), _ => { })).Disable(true);
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.Shared == null || MoveableObject.Shared.Mode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.Shared?.Mode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.Shared?.Mode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        builder.AddField("Model Identifier", builder.AddDropdown(_Identifiers, _SceneryAssetInstanceIdentifierIndex, o => {
            _SceneryAssetInstanceIdentifierIndex = o;
            sceneryAssetInstance.gameObject.SetActive(false);
            sceneryAssetInstance.identifier = _Identifiers[o]!;
            sceneryAssetInstance.gameObject.SetActive(true);
            MapEditorPlugin.PatchEditor.AddOrUpdateScenery(sceneryAssetInstance.name, new SerializedScenery(sceneryAssetInstance));
        }));

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            MoveableObject.Create(sceneryAssetInstance.gameObject, mode, (startPosition, startRotation) => OnComplete(sceneryAssetInstance, startPosition, startRotation));
            strip.Rebuild();
        };
    }

    private void OnComplete(SceneryAssetInstance sceneryAssetInstance, Vector3 startPosition, Quaternion startRotation) {
        MapEditorPlugin.PatchEditor.AddOrUpdateScenery(sceneryAssetInstance.name, new SerializedScenery(sceneryAssetInstance));
    }
}
