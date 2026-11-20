namespace UyapEditorMcp;

public sealed record PageResult(int Page, int PageCount, string ObservedThrough);
public sealed record OpenResult(string DocumentId, string FileName, string Version, bool Editable, string? InputSha256, PageResult? Position,
    string Mode = "document", string? SignatureStatus = null);
public sealed record PdfResult(string Path, int PageCount, long Bytes, string Sha256, string EditorCommand, bool SourceUnchanged);
public sealed record SavedCopy(string Path, long Bytes, string Sha256);
public sealed record NativeBackup(string Path, string Name, long Bytes, DateTime ModifiedUtc, string Sha256);
public sealed record NativeBackups(int Offset, NativeBackup[] Backups, int? NextOffset);
public sealed record BackupRecovery(SavedCopy Output, string EditorCommand);
public sealed record StatusResult(string EditorVersion, string Platform, int ManagedDocuments, string Bridge, string[] Capabilities);
public sealed record TextResult(int Start, int TotalCharacters, string Text);
public sealed record CapitalizationState(bool FirstLetterConfigured, bool FirstLetterActive, bool UppercaseConfigured,
    bool UppercaseActive);
public sealed record PageRegion(string Region, bool Present, int Start, int End, string Text);
public sealed record PageRegionRange(string Region, int FirstPage, int? LastPage);
public sealed record PageRegionsResult(PageRegion[] Regions);
public sealed record SelectionResult(int Start, int End);
public sealed record FindResult(bool Found, int Start, int End);
public sealed record EditorViewResult(int ZoomPercent, bool ShowWhitespace, int Caret, int SelectionStart,
    int SelectionEnd, int TotalCharacters, double RenderScale, int[] ZoomOptions, bool ShowComments);
public sealed record EditorFontResult(int Start, int End, string? Family, int? Size);
public sealed record PaletteColorResult(string Target, string Color, string Scope, int? Start, int? End);
public sealed record LineSpacingResult(double Multiplier, int Start, int End);
public sealed record FormatBrushResult(int SourceOffset, int Start, int End, bool IncludesParagraph);
public sealed record ListPaletteResult(string Preset, string? NativeStyle, int Start, int End);
public sealed record ListLevelFormat(int Level, string Format);
public sealed record ListLevelIndent(int Level, float IndentPoints);
public sealed record ListIndentsResult(int ItemCount);
public sealed record CustomListResult(int ItemCount, bool Created);
public sealed record ListItemResult(int Start, int End, int? Level, bool Numbered, int? ExplicitStart,
    ListLevelFormat[]? LevelFormats, float? IndentPoints);
public sealed record EditorShortcut(string Action, string Label, string Key);
public sealed record EditorShortcutsResult(int Total, int Offset, EditorShortcut[] Shortcuts);
public sealed record SpellingIssue(int Start, int End, string Word, string[] Suggestions);
public sealed record SpellingResult(int TotalCharacters, int NextStart, bool Complete, SpellingIssue[] Issues);
public sealed record EditResult(int CharacterCount);
public sealed record EditorComment(int Index, int Start, int End, int Type, string Text);
public sealed record CommentsResult(int Total, int Offset, EditorComment[] Comments);
public sealed record AutomaticTextEntry(string Key, string Kind, string Text);
public sealed record AutomaticTexts(AutomaticTextEntry[] Entries);
public sealed record AutomaticTextInsertion(string Key, string Kind, int Start, int End);
public sealed record CommentChangeResult(int Total, int Index, int Type, bool Deleted);
public sealed record UdfChange(int Start, int End, string Kind, string Text, string ChangeId, int GroupStart, int GroupEnd);
public sealed record UdfChangeNavigation(string Action, bool Found, int? Start, int? End, string? ChangeId,
    string? ObservedThrough);
public sealed record UdfComparison(string Decision, int Total, UdfChange[] Changes, SavedCopy? Output,
    string? OtherGroupsFrom, int Offset, int? NextOffset, UdfChangeNavigation? Navigation);
public sealed record ActionResult(string Action, int SelectionStart, int SelectionEnd, string? Diagnostic = null);
public sealed record ActionsResult(int Total, int Offset, string[] Actions, string ObservedThrough);
public sealed record RibbonButton(string Label, bool Enabled);
public sealed record RibbonResult(string[] Tabs, string? SelectedTab, RibbonButton[] Commands);
public sealed record CatalogEntry(string[] Path, string Label, string State, string? Action, string Kind);
public sealed record CatalogResult(string Mode, string Scope, int Total, int Offset, CatalogEntry[] Entries);
public sealed record ManagedWindow(string DocumentId, string FileName, string Mode, bool Editable);
public sealed record ManagedWindows(ManagedWindow[] Windows);
public sealed record CloseResult(string DocumentId, bool Closed);
public sealed record EditModeResult(bool Editable, bool NativeEditable, bool CanEnableEditing);
public sealed record DialogControl(int Id, string Role, string Label, string Value, string[] Options);
public sealed record DialogResult(string State, string? Title, string? Revision, DialogControl[] Controls);
public sealed record FormatExport(string Path, string Format, long Bytes, string Sha256, string EditorCommand);
public sealed record ImageResult(string FileName, string Mode, string State);
public sealed record MacroResult(int RecordedSteps, int SelectionStart, int SelectionEnd);
public sealed record BatchFile(string Path, string OutputPath);
public sealed record BatchItem(string FileName, bool Succeeded, string? OutputPath, string? Error, string? OpenDocumentId);
public sealed record BatchResult(int Completed, BatchItem[] Files);
public sealed record ManualWorkflow(string Workflow, string Tab, string EditorCommand, bool Enabled, string Status);
public sealed record AttachmentPreparation(string Action, string Format, string Path, long Bytes, string Sha256,
    string EditorCommand, string Status);
public sealed record EditorFormats(string[] OpenFilters, string[] SaveFilters);
