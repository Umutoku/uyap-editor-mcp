namespace UyapEditorMcp;

public sealed record UdfChange(int Start, int End, string Kind, string Text, string ChangeId, int GroupStart, int GroupEnd);
