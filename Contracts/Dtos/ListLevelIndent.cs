namespace UyapEditorMcp;

public sealed class ListLevelIndent(int level, float indentPoints)
{
    public int Level { get; } = level;
    public float IndentPoints { get; } = indentPoints;
}
