using System;
using System.Linq;
using Core;
using HarmonyLib;
using MapEditor.Behaviours;
using MapEditor.MapState;
using MapEditor.MapState.TelegraphPoleEditor;
using MapEditor.MapState.TrackNodeEditor;
using MapEditor.MapState.TrackSegmentEditor;
using Track;
using UI.Builder;
using UnityEngine;

namespace MapEditor.UI.Controls;

public static class AssetsEditor
{
    public static void Build(UIPanelBuilder builder, ImmutableArray array) {
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.ActiveMode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.ActiveMode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.ActiveMode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            InitMoveableObject(array, mode);
            strip.Rebuild();
        };
    }

    private static void InitMoveableObject(ImmutableArray array, MoveableObjectMode mode) {
        var handlers = new IMoveableObjectHandler[array.Length];
        for (var i = 0; i < array.Length; i++) {
            handlers[i] = CreateHandler(array[i], mode);
        }

        MoveableObject.Create(new ArrayMoveHandler(handlers));
    }

    private static IMoveableObjectHandler CreateHandler(object entity, MoveableObjectMode mode) {
        return entity switch {
            TrackNode trackNode             => new TrackNodeMoveHandler(trackNode, mode),
            TrackSegment trackSegment       => new TrackSegmentMoveHandler(trackSegment, mode),
            TelegraphPoleId telegraphPoleId => new TelegraphPoleModeHandler(telegraphPoleId.Id, mode),
            _                               => throw new NotImplementedException()
        };
    }
}

internal sealed class ArrayMoveHandler(IMoveableObjectHandler[] handlers) : IMoveableObjectHandler
{
    public GameObject GameObject => handlers[0].GameObject;

    public MoveableObjectMode Mode => handlers[0].Mode;

    public Vector3    StartPosition => handlers[0].StartPosition;
    public Quaternion StartRotation => handlers[0].StartRotation;

    public void OnStart() {
        handlers.Do(o => o.OnStart());
    }

    public void OnUpdate(Vector3? translation, Quaternion? rotation) {
        handlers.Do(o => o.OnUpdate(translation, rotation));
    }

    public IStateStep OnComplete(Vector3? translation, Quaternion? rotation) {
        return new CompoundSteps(handlers.Select(o => o.OnComplete(translation, rotation)));
    }
}
