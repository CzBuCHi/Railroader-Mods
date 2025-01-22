﻿using MapEditor.Behaviours;
using MapEditor.Utility;
using Serilog;
using Track;
using UnityEngine;

namespace MapEditor.MapState.TelegraphPoleEditor;

public sealed class TelegraphPoleModeHandler(int telegraphPoleId, MoveableObjectMode mode) : IMoveableObjectHandler
{
    public GameObject         GameObject => TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId).gameObject;
    public MoveableObjectMode Mode       { get; } = mode;

    private Vector3    _StartPosition;
    private Quaternion _StartRotation;

    public void OnStart() {
        
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);
        _StartPosition = telegraphPole.transform.localPosition;
        _StartRotation = telegraphPole.transform.localRotation;
    }

    public void OnUpdate(Vector3? translation, Quaternion? rotation) {
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);

        if (translation.HasValue) {
            telegraphPole.transform.localPosition = _StartPosition + translation.Value;
        }

        if (rotation.HasValue) {
            telegraphPole.transform.localRotation = _StartRotation * rotation.Value;
        }
    }

    public IStateStep OnComplete(Vector3? translation, Quaternion? rotation) {
        var node = TelegraphPoleUtility.Graph.NodeForId(telegraphPoleId)!;

        UnityHelpers.CallOnNextFrame(() => MoveableObject.Create(this));

        return new TelegraphPoleUpdate(telegraphPoleId) {
            OriginalPosition = translation != null ? node.position : null,
            OriginalRotation = rotation != null ? node.eulerAngles : null,
            Position = translation != null ? node.position + translation.Value : null,
            Rotation = rotation != null ? node.eulerAngles = rotation.Value.eulerAngles : null
        };
    }
}
