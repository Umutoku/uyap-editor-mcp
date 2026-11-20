namespace UyapEditorMcp;

public sealed record DialogResult(string State, string? Title, string? Revision, DialogControl[] Controls);
