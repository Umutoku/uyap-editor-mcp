using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace UyapEditorMcp;

[McpServerToolType]
internal sealed class EditorTools(LocalEditor editor, LocalArchiveService archives)
{
    [McpServerTool(Name = McpToolNames.Status, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Status))]
    public Task<StatusResult> Status(CancellationToken cancellationToken) => Run(() => editor.Status(cancellationToken));

    [McpServerTool(Name = McpToolNames.Open, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Open))]
    public Task<OpenResult> Open(string path, bool editable = false, string? capitalizationFromDocumentId = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Open(path, editable, cancellationToken, capitalizationFromDocumentId));

    [McpServerTool(Name = McpToolNames.CapitalizationState, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.CapitalizationState))]
    public Task<CapitalizationState> Capitalization(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ReadCapitalization(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.ImportDocument, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ImportDocument))]
    public Task<OpenResult> ImportDocument(string path, CancellationToken cancellationToken) =>
        Run(() => editor.ImportDocument(path, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListFormats, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListFormats))]
    public Task<EditorFormats> ListFormats(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ListFormats(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.New, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.New))]
    public Task<OpenResult> New(CancellationToken cancellationToken) => Run(() => editor.New(cancellationToken));

    [McpServerTool(Name = McpToolNames.OpenTemplate, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.OpenTemplate))]
    public Task<OpenResult> OpenTemplate(string? path = null, CancellationToken cancellationToken = default) =>
        Run(() => editor.OpenTemplate(path, cancellationToken));

    [McpServerTool(Name = McpToolNames.ReuseTemplateField, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ReuseTemplateField))]
    public Task<EditResult> ReuseTemplateField(string documentId, string fieldName, CancellationToken cancellationToken) =>
        Run(() => editor.ReuseTemplateField(documentId, fieldName, cancellationToken));

    [McpServerTool(Name = McpToolNames.GetPage, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.GetPage))]
    public Task<PageResult> GetPage(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.Page(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.ViewState, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ViewState))]
    public Task<EditorViewResult> ViewState(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ViewState(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetView, UseStructuredContent = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetView))]
    public Task<EditorViewResult> SetView(string documentId, int? zoomPercent = null, bool? showWhitespace = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetView(documentId, zoomPercent, showWhitespace, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetFont, UseStructuredContent = true, Destructive = true, Idempotent = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetFont))]
    public Task<EditorFontResult> SetFont(string documentId, string? family = null, int? size = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetFont(documentId, family, size, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetPaletteColor, UseStructuredContent = true, Destructive = true, Idempotent = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetPaletteColor))]
    public Task<PaletteColorResult> SetPaletteColor(string documentId, string target, string color,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetPaletteColor(documentId, target, color, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetLineSpacing, UseStructuredContent = true, Destructive = true, Idempotent = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetLineSpacing))]
    public Task<LineSpacingResult> SetLineSpacing(string documentId, double multiplier,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetLineSpacing(documentId, multiplier, cancellationToken));

    [McpServerTool(Name = McpToolNames.ApplyFormatBrush, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ApplyFormatBrush))]
    public Task<FormatBrushResult> ApplyFormatBrush(string documentId, int sourceOffset, int start, int end,
        bool includeParagraph = false, CancellationToken cancellationToken = default) =>
        Run(() => editor.ApplyFormatBrush(documentId, sourceOffset, start, end, includeParagraph, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetListPalette, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetListPalette))]
    public Task<ListPaletteResult> SetListPalette(string documentId, string preset,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetListPalette(documentId, preset, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetCustomList, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetCustomList))]
    public Task<CustomListResult> SetCustomList(string documentId, ListLevelFormat[] levels,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetCustomList(documentId, levels, cancellationToken));

    [McpServerTool(Name = McpToolNames.GetListItem, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.GetListItem))]
    public Task<ListItemResult> GetListItem(string documentId, CancellationToken cancellationToken = default) =>
        Run(() => editor.GetListItem(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetListLevel, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetListLevel))]
    public Task<ListItemResult> SetListLevel(string documentId, int level,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetListLevel(documentId, level, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetListIndents, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetListIndents))]
    public Task<ListIndentsResult> SetListIndents(string documentId, ListLevelIndent[] levels,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetListIndents(documentId, levels, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetNumberingStart, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetNumberingStart))]
    public Task<ListItemResult> SetNumberingStart(string documentId, int startAt,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetNumberingStart(documentId, startAt, cancellationToken));

    [McpServerTool(Name = McpToolNames.ContinueNumbering, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ContinueNumbering))]
    public Task<ListItemResult> ContinueNumbering(string documentId, CancellationToken cancellationToken = default) =>
        Run(() => editor.ContinueNumbering(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListShortcuts, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListShortcuts))]
    public Task<EditorShortcutsResult> ListShortcuts(string documentId, int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Shortcuts(documentId, offset, limit, cancellationToken));

    [McpServerTool(Name = McpToolNames.GoToPage, UseStructuredContent = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.GoToPage))]
    public Task<PageResult> GoToPage(string documentId, int page, CancellationToken cancellationToken) =>
        Run(() => editor.Go(documentId, page, cancellationToken));

    [McpServerTool(Name = McpToolNames.ExportPdf, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ExportPdf))]
    public Task<PdfResult> ExportPdf(string documentId, string outputPath, CancellationToken cancellationToken) =>
        Run(() => editor.ExportPdf(documentId, outputPath, cancellationToken));

    [McpServerTool(Name = McpToolNames.ExportSignedImz, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ExportSignedImz))]
    public Task<SignedImzExport> ExportSignedImz(string documentId, string outputPath, CancellationToken cancellationToken) =>
        Run(() => editor.ExportSignedImz(documentId, outputPath, cancellationToken));

    [McpServerTool(Name = McpToolNames.Encrypt7z, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Encrypt7z))]
    public Task<LocalArchiveResult> Encrypt7z(string udfPath, string outputPath, string passwordFile, CancellationToken cancellationToken) =>
        Run(() => archives.Encrypt7z(udfPath, outputPath, passwordFile, cancellationToken));

    [McpServerTool(Name = McpToolNames.Extract7z, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Extract7z))]
    public Task<LocalArchiveResult> Extract7z(string archivePath, string outputPath, string passwordFile, CancellationToken cancellationToken) =>
        Run(() => archives.Extract7z(archivePath, outputPath, passwordFile, cancellationToken));

    [McpServerTool(Name = McpToolNames.SaveCopy, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SaveCopy))]
    public Task<SavedCopy> SaveCopy(string documentId, string outputPath, CancellationToken cancellationToken) =>
        Run(() => editor.SaveCopy(documentId, outputPath, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListBackups, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListBackups))]
    public Task<NativeBackups> ListBackups(int offset = 0, int limit = 20, CancellationToken cancellationToken = default) =>
        Run(() => editor.ListBackups(offset, limit, cancellationToken));

    [McpServerTool(Name = McpToolNames.RestoreBackup, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.RestoreBackup))]
    public Task<BackupRecovery> RestoreBackup(string backupPath, string expectedSha256, string outputPath,
        CancellationToken cancellationToken) =>
        Run(() => editor.RestoreBackup(backupPath, expectedSha256, outputPath, cancellationToken));

    [McpServerTool(Name = McpToolNames.ExportDocument, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ExportDocument))]
    public Task<FormatExport> ExportDocument(string documentId, string outputPath, bool htmlPageBreaks = false,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Export(documentId, outputPath, cancellationToken, htmlPageBreaks));

    [McpServerTool(Name = McpToolNames.BatchExport, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.BatchExport))]
    public Task<BatchResult> BatchExport(BatchFile[] files, CancellationToken cancellationToken) =>
        Run(() => editor.Batch(files, cancellationToken));

    [McpServerTool(Name = McpToolNames.ReadText, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ReadText))]
    public Task<TextResult> ReadText(string documentId, int start = 0, int limit = 2048,
        CancellationToken cancellationToken = default) => Run(() => editor.ReadText(documentId, start, limit, cancellationToken));

    [McpServerTool(Name = McpToolNames.PageRegions, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.PageRegions))]
    public Task<PageRegionsResult> ReadPageRegions(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ReadPageRegions(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetPageRegion, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetPageRegion))]
    public Task<PageRegion> SetPageRegion(string documentId, string region, string text,
        CancellationToken cancellationToken) =>
        Run(() => editor.SetPageRegion(documentId, region, text, cancellationToken));

    [McpServerTool(Name = McpToolNames.PageRegionRange, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.PageRegionRange))]
    public Task<PageRegionRange> PageRegionRange(string documentId, string region, int? firstPage = null,
        int? lastPage = null, CancellationToken cancellationToken = default) =>
        Run(() => editor.PageRegionRange(documentId, region, firstPage, lastPage, cancellationToken));

    [McpServerTool(Name = McpToolNames.SelectText, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SelectText))]
    public Task<SelectionResult> SelectText(string documentId, int start, int end, CancellationToken cancellationToken) =>
        Run(() => editor.SelectText(documentId, start, end, cancellationToken));

    [McpServerTool(Name = McpToolNames.CompareUdf, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.CompareUdf))]
    public Task<UdfComparison> CompareUdf(string documentId, string baselinePath, string decision = "inspect",
        string? outputPath = null, int limit = 20, string? changeId = null, int offset = 0,
        string? navigationDirection = null, string? navigationFromChangeId = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.CompareUdf(documentId, baselinePath, decision, outputPath, limit, changeId, offset,
            navigationDirection, navigationFromChangeId, cancellationToken));

    [McpServerTool(Name = McpToolNames.Find, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Find))]
    public Task<FindResult> Find(string documentId, string query, int start = 0, bool caseSensitive = true,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Find(documentId, query, start, caseSensitive, cancellationToken));

    [McpServerTool(Name = McpToolNames.SpellCheck, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SpellCheck))]
    public Task<SpellingResult> SpellCheck(string documentId, int start = 0, int characterLimit = 2048, int issueLimit = 20,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SpellCheck(documentId, start, characterLimit, issueLimit, cancellationToken));

    [McpServerTool(Name = McpToolNames.ApplySpelling, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ApplySpelling))]
    public Task<EditResult> ApplySpelling(string documentId, int start, int end, string expectedWord, string suggestion,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.ApplySpelling(documentId, start, end, expectedWord, suggestion, cancellationToken));

    [McpServerTool(Name = McpToolNames.ReplaceText, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ReplaceText))]
    public Task<EditResult> ReplaceText(string documentId, int start, int end, string text, CancellationToken cancellationToken) =>
        Run(() => editor.Replace(documentId, start, end, text, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListAutomaticText, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListAutomaticText))]
    public Task<AutomaticTexts> ListAutomaticText(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ListAutomaticText(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.SetAutomaticText, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.SetAutomaticText))]
    public Task<AutomaticTexts> SetAutomaticText(string documentId, string key, string kind = "plain",
        string operation = "set", string? text = null, string? richSourcePath = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetAutomaticText(documentId, key, kind, operation, text, richSourcePath, cancellationToken));

    [McpServerTool(Name = McpToolNames.InsertAutomaticText, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.InsertAutomaticText))]
    public Task<AutomaticTextInsertion> InsertAutomaticText(string documentId, string key, string kind = "plain",
        CancellationToken cancellationToken = default) =>
        Run(() => editor.InsertAutomaticText(documentId, key, kind, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListComments, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListComments))]
    public Task<CommentsResult> ListComments(string documentId, int offset = 0, int limit = 20,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.ListComments(documentId, offset, limit, cancellationToken));

    [McpServerTool(Name = McpToolNames.AddComment, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.AddComment))]
    public Task<EditorComment> AddComment(string documentId, int start, int end, string text, int type = 1,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.AddComment(documentId, start, end, text, type, cancellationToken));

    [McpServerTool(Name = McpToolNames.EditComment, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.EditComment))]
    public Task<EditorComment> EditComment(string documentId, int index, int expectedStart, int expectedEnd,
        string expectedText, string text, CancellationToken cancellationToken) =>
        Run(() => editor.EditComment(documentId, index, expectedStart, expectedEnd, expectedText, text, cancellationToken));

    [McpServerTool(Name = McpToolNames.ChangeComment, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ChangeComment))]
    public Task<CommentChangeResult> ChangeComment(string documentId, int index, int expectedStart, int expectedEnd,
        int expectedType, string expectedText, string operation, CancellationToken cancellationToken) =>
        Run(() => editor.ChangeComment(documentId, index, expectedStart, expectedEnd, expectedType, expectedText,
            operation, cancellationToken));

    [McpServerTool(Name = McpToolNames.InsertImage, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.InsertImage))]
    public Task<ImageResult> InsertImage(string documentId, string path, bool background = false,
        CancellationToken cancellationToken = default) => Run(() => editor.InsertImage(documentId, path, background, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListActions, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListActions))]
    public Task<ActionsResult> ListActions(string documentId, int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default) => Run(() => editor.Actions(documentId, offset, limit, cancellationToken));

    [McpServerTool(Name = McpToolNames.MenuInventory, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.MenuInventory))]
    public Task<CatalogResult> Catalog(string documentId, string scope = "menus", int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Catalog(documentId, scope, offset, limit, cancellationToken));

    [McpServerTool(Name = McpToolNames.ListWindows, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.ListWindows))]
    public Task<ManagedWindows> ListWindows(CancellationToken cancellationToken = default) =>
        Run(() => editor.Windows(cancellationToken));

    [McpServerTool(Name = McpToolNames.Ribbon, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Ribbon))]
    public Task<RibbonResult> Ribbon(string documentId, string? tab = null, CancellationToken cancellationToken = default) =>
        Run(() => editor.Ribbon(documentId, tab, cancellationToken));

    [McpServerTool(Name = McpToolNames.RunAction, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.RunAction))]
    public Task<ActionResult> RunAction(string documentId, string action, string? text = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Action(documentId, action, text, cancellationToken));

    [McpServerTool(Name = McpToolNames.RunMacro, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.RunMacro))]
    public Task<MacroResult> RunMacro(string documentId, string[] actions, int recordStart, int recordEnd,
        int replayStart, int replayEnd, CancellationToken cancellationToken) =>
        Run(() => editor.Macro(documentId, actions, recordStart, recordEnd, replayStart, replayEnd, cancellationToken));

    [McpServerTool(Name = McpToolNames.OpenForm, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.OpenForm))]
    public Task<DialogResult> OpenForm(string documentId, string action, CancellationToken cancellationToken) =>
        Run(() => editor.OpenForm(documentId, action, cancellationToken));

    [McpServerTool(Name = McpToolNames.InspectForm, UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.InspectForm))]
    public Task<DialogResult> InspectForm(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.InspectForm(documentId, cancellationToken));

    [McpServerTool(Name = McpToolNames.FormInput, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.FormInput))]
    public Task<DialogResult> FormInput(string documentId, string revision, int controlId, string operation,
        string value = "", CancellationToken cancellationToken = default) =>
        Run(() => editor.FormInput(documentId, revision, controlId, operation, value, cancellationToken));

    [McpServerTool(Name = McpToolNames.PrepareWorkflow, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.PrepareWorkflow))]
    public Task<ManualWorkflow> PrepareWorkflow(string documentId, string workflow, CancellationToken cancellationToken) =>
        Run(() => editor.Handoff(documentId, workflow, cancellationToken));

    [McpServerTool(Name = McpToolNames.PrepareAttachment, UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.PrepareAttachment))]
    public Task<AttachmentPreparation> PrepareAttachment(string documentId, string action, string outputPath,
        CancellationToken cancellationToken) =>
        Run(() => editor.PrepareAttachment(documentId, action, outputPath, cancellationToken));

    [McpServerTool(Name = McpToolNames.PrepareApproval, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.PrepareApproval))]
    public Task<ApprovalReview> PrepareApproval(ApprovalRequest request, CancellationToken cancellationToken = default) =>
        Run(() => editor.PrepareApproval(request, cancellationToken));

    [McpServerTool(Name = McpToolNames.EditMode, UseStructuredContent = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.EditMode))]
    public Task<EditModeResult> EditMode(string documentId, bool? editable = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.EditMode(documentId, editable, cancellationToken));

    [McpServerTool(Name = McpToolNames.Close, UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [ResourceDescription(nameof(McpToolNames.Close))]
    public Task<CloseResult> Close(string documentId, CancellationToken cancellationToken) => Run(() => editor.Close(documentId, cancellationToken));

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (EditorException error) { throw ToMcpError(error); }
        catch (UnauthorizedAccessException) { throw ToMcpError(new EditorException(EditorErrors.AccessDenied)); }
        catch (IOException) { throw ToMcpError(new EditorException(EditorErrors.LocalIoError)); }
    }

    private static McpException ToMcpError(EditorException error) => new($"{error.Code}: {error.Message}");
}
