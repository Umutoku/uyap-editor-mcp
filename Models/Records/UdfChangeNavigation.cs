namespace UyapEditorMcp;

public sealed record UdfChangeNavigation(string Action, bool Found, int? Start, int? End, string? ChangeId,
    string? ObservedThrough);
