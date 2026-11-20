namespace UyapEditorMcp;

public sealed class ListLevelFormat(int level, string format)
{
    public int Level { get; } = level;
    public string Format { get; } = format;
}
