namespace MapEditor.MapState;

public interface IStateStep
{
    void Do();
    void Undo();

#if DEBUG
    string DoText   { get; }
    string UndoText { get; }
#endif
}
