using System.Linq;
using System.Text;

namespace MapEditor.MapState;

public sealed record CompoundSteps(params IStateStep[] Steps) : IStateStep
{
    public void Do() {
        foreach (var step in Steps) {
            step.Do();
        }
    }

    public void Undo() {
        foreach (var step in Steps.Reverse()) {
            step.Undo();
        }
    }

#if DEBUG
    public string DoText {
        get {
            var sb = new StringBuilder().Append("CompoundSteps [");
            foreach (var step in Steps) {
                sb.AppendLine("  ").Append(step.DoText);
            }

            return sb.AppendLine().Append(']').ToString();
        }
    }

    public string UndoText {
        get {
            var sb = new StringBuilder().Append("CompoundSteps [");
            foreach (var step in Steps.Reverse()) {
                sb.AppendLine("  ").Append(step.UndoText);
            }

            return sb.AppendLine().Append(']').ToString();
        }
    }
#endif
}
