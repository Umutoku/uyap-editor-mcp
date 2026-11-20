namespace UyapEditorMcp;

public sealed record SpellingResult(int TotalCharacters, int NextStart, bool Complete, SpellingIssue[] Issues);
