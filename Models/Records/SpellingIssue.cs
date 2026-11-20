namespace UyapEditorMcp;

public sealed record SpellingIssue(int Start, int End, string Word, string[] Suggestions);
