using System;
using MapEditor.Behaviours;
using MapEditor.Harmony;
using MapEditor.Utility;
using Serilog;
using Track;
using UI.Builder;
using UnityEngine;

namespace MapEditor.UI;

public sealed partial class EditorWindow
{
    

    private Vector3    _TelegraphPoleOriginalPosition;
    private Quaternion _TelegraphPoleOriginalInverseRotation;

    private void BuildTelegraphPoleEditor(UIPanelBuilder builder, int telegraphPoleId) {
        builder.AddField("Id", builder.AddInputField("", _ => { })).Disable(true);
        builder.AddField("Position", builder.AddInputField(TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId).transform.localPosition.ToString(), _ => { })).Disable(true);
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.Shared == null || MoveableObject.Shared.Mode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.Shared?.Mode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.Shared?.Mode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            ConfigureMoveableObject(telegraphPoleId, mode);
            strip.Rebuild();
        };
    }

    private void ConfigureMoveableObject(int telegraphPoleId, MoveableObjectMode mode) {
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);

        _TelegraphPoleOriginalPosition = telegraphPole.transform.localPosition;
        _TelegraphPoleOriginalInverseRotation = Quaternion.Inverse(telegraphPole.transform.localRotation);

        MoveableObject.Create(telegraphPole.gameObject, mode, (startPosition, startRotation) => OnComplete(telegraphPoleId, mode, startPosition, startRotation));
    }

    private void OnComplete(int telegraphPoleId, MoveableObjectMode objectMode, Vector3 startPosition, Quaternion startRotation) {
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);
        var node          = TelegraphPoleUtility.Graph.NodeForId(telegraphPoleId)!;

        var deltaPosition = _TelegraphPoleOriginalPosition - telegraphPole.transform.localPosition;
        var deltaRotation = _TelegraphPoleOriginalInverseRotation * telegraphPole.transform.localRotation;

        node.position -= deltaPosition;
        node.eulerAngles += deltaRotation.eulerAngles;
        
        TelegraphPoleUtility.Manager.Rebuild();
        UnityHelpers.CallOnNextFrame(() => ConfigureMoveableObject(telegraphPoleId, objectMode));
    }
}
