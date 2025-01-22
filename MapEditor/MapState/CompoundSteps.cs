using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapEditor.MapState;

public sealed record CompoundSteps(params IEnumerable<IStateStep> Steps) : IStateStep
{
    private readonly IStateStep[] _Steps = Steps.ToArray();

    public void Do() {
        foreach (var step in _Steps) {
            step.Do();
        }
    }

    public void Undo() {
        foreach (var step in _Steps.Reverse()) {
            step.Undo();
        }
    }

#if DEBUG
    public string DoText {
        get {
            var sb = new StringBuilder().Append("CompoundSteps [");
            foreach (var step in _Steps) {
                sb.AppendLine("  ").Append(step.DoText);
            }

            return sb.AppendLine().Append(']').ToString();
        }
    }

    public string UndoText {
        get {
            var sb = new StringBuilder().Append("CompoundSteps [");
            foreach (var step in _Steps.Reverse()) {
                sb.AppendLine("  ").Append(step.UndoText);
            }

            return sb.AppendLine().Append(']').ToString();
        }
    }
#endif
}
