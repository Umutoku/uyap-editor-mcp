namespace UyapEditorMcp;

public sealed record UdfComparison(string Decision, int Total, UdfChange[] Changes, SavedCopy? Output,
    string? OtherGroupsFrom, int Offset, int? NextOffset, UdfChangeNavigation? Navigation);
