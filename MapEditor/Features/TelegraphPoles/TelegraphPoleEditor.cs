using System;
using MapEditor.Behaviours;
using UI.Builder;

namespace MapEditor.Features.TelegraphPoles;

public static class TelegraphPoleEditor
{
    public static void Build(UIPanelBuilder builder, int telegraphPoleId) {
        builder.AddField("Id", builder.AddInputField("", _ => { })).Disable(true);
        builder.AddField("Position", builder.AddInputField(TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId).transform.localPosition.ToString(), _ => { })).Disable(true);
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.ActiveMode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.ActiveMode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.ActiveMode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            MoveableObject.Create(new TelegraphPoleModeHandler(telegraphPoleId, mode));
            strip.Rebuild();
        };
    }
}
