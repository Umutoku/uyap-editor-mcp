namespace UyapEditorMcp;

public sealed record ActionResult(string Action, int SelectionStart, int SelectionEnd, string? Diagnostic = null);
