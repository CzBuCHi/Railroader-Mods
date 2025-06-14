using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using MapEditor.Behaviours;
using MapEditor.Extensions;
using StrangeCustoms.Tracks;
using UI.Builder;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapEditor.UI.Controls;

public static class SceneryAssetInstanceEditor
{
    private static List<string>? _Identifiers;

    private static int _SceneryAssetInstanceIdentifierIndex;

    public static void Build(UIPanelBuilder builder, SceneryAssetInstance sceneryAssetInstance) {
        _Identifiers ??= Object.FindObjectsByType<SceneryAssetInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None)!
                               .Select(o => o.identifier).Distinct().OrderBy(o => o).ToList();

        builder.AddField("Id", builder.AddInputField(sceneryAssetInstance.name!, _ => { })).Disable(true);
        builder.AddField("Position", builder.AddInputField(sceneryAssetInstance.transform.localPosition.ToString(), _ => { })).Disable(true);
        builder.AddField("Rotation", builder.AddInputField(sceneryAssetInstance.transform.localEulerAngles.ToString(), _ => { })).Disable(true);
        //builder.AddField("Operation", builder.ButtonStrip(strip => {
        //    strip.AddButtonSelectable("None", MoveableObject.ActiveMode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
        //    strip.AddButtonSelectable("Move", MoveableObject.ActiveMode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
        //    strip.AddButtonSelectable("Rotate", MoveableObject.ActiveMode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        //}));

        builder.AddField("Model Identifier", builder.AddDropdown(_Identifiers, _SceneryAssetInstanceIdentifierIndex, o => {
            _SceneryAssetInstanceIdentifierIndex = o;
            sceneryAssetInstance.gameObject.SetActive(false);
            sceneryAssetInstance.identifier = _Identifiers[o]!;
            sceneryAssetInstance.gameObject.SetActive(true);
            MapEditorPlugin.PatchEditor!.AddOrUpdateScenery(sceneryAssetInstance.name!, new SerializedScenery(sceneryAssetInstance));
        }));

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            //MapEditorPlugin.PatchEditor.AddOrUpdateScenery(sceneryAssetInstance.name, new SerializedScenery(sceneryAssetInstance));
            strip.Rebuild();
        };
    }
}
