using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using MapEditor.Events;
using MapEditor.Utility;
using Serilog;
using Track;

namespace MapEditor.MapState;

public static class MapStateEditor
{
    private static readonly List<IStateStep> _Steps = new();
    private static          int              _Index;

    public static string Steps() => _Steps.Count > 0 ? $"{_Index} / {_Steps.Count}" : "None";

    public static bool CanUndo => _Steps.Count > 0 && _Index > 0;
    public static bool CanRedo => _Steps.Count > 0 && _Index < _Steps.Count;
    public static int  Count   => _Steps.Count;

    public static void Undo() {
        --_Index;
#if DEBUG
        Log.Information($"MapEditor <<< [{_Index}] {_Steps[_Index]!.UndoText}");
#endif
        _Steps[_Index]!.Undo();
        Notify();
    }

    public static void Redo() {
#if DEBUG
        Log.Information($"MapEditor >>> [{_Index}] {_Steps[_Index]!.DoText}");
#endif
        _Steps[_Index]!.Do();
        ++_Index;
        Notify();
    }

    public static void UndoAll() {
        while (_Index > 0) {
            --_Index;
#if DEBUG
            Log.Information($"MapEditor <<< [{_Index}] {_Steps[_Index]!.UndoText}");
#endif
            _Steps[_Index]!.Undo();
        }

        Notify();
    }

    public static void RedoAll() {
        while (_Index < _Steps.Count - 1) {
#if DEBUG
            Log.Information($"MapEditor >>> [{_Index}] {_Steps[_Index]!.DoText}");
#endif
            _Steps[_Index]!.Do();
            ++_Index;
        }

        Notify();
    }

    public static void Clear() {
        _Steps.Clear();
        _Index = 0;
        Notify();
    }

    public static void NextSteps(params IStateStep[] steps) {
        NextStep(new CompoundSteps(steps));
    }

    public static void NextStep(IStateStep step) {
        if (_Steps.Count > 0 && _Index < _Steps.Count) {
            _Steps.RemoveRange(_Index, _Steps.Count - _Index);
        }

        _Steps.Add(step);
        Redo();
    }

    private static void Notify() {
        Messenger.Default.Send(new UndoRedoChanged());
        UnityHelpers.CallOnNextFrame(TrackObjectManager.Instance.Rebuild);
    }
}
