namespace UyapEditorMcp;

public sealed record ListItemResult(int Start, int End, int? Level, bool Numbered, int? ExplicitStart,
    ListLevelFormat[]? LevelFormats, float? IndentPoints);
