using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace UyapEditorMcp;

[McpServerToolType]
internal sealed class EditorTools(LocalEditor editor)
{
    [McpServerTool(Name = "uyap_status", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Return the verified local UYAP Editor version, managed session count and accessibility status.")]
    public Task<StatusResult> Status(CancellationToken cancellationToken) => Run(() => editor.Status(cancellationToken));

    [McpServerTool(Name = "uyap_open", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Open a UDF from the configured input or output folder in a new managed UYAP Editor instance. Returns a document ID and verified page position. Uses a local copy. Optionally copy the two capitalization settings from a currently open managed document into the new profile.")]
    public Task<OpenResult> Open(string path, bool editable = false, string? capitalizationFromDocumentId = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Open(path, editable, cancellationToken, capitalizationFromDocumentId));

    [McpServerTool(Name = "uyap_capitalization_state", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read the two configured capitalization options and whether their native keyboard handlers are active in this managed editor instance. Changes from the capitalization forms take effect in a new editor opened with capitalizationFromDocumentId.")]
    public Task<CapitalizationState> Capitalization(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ReadCapitalization(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_import_document", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Open native UYAP XML or USF, or import RTF via the editor's Open dialog, into a new read-only managed document session.")]
    public Task<OpenResult> ImportDocument(string path, CancellationToken cancellationToken) =>
        Run(() => editor.ImportDocument(path, cancellationToken));

    [McpServerTool(Name = "uyap_list_formats", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Read the current native Open and Save As chooser filters without opening or writing a file.")]
    public Task<EditorFormats> ListFormats(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ListFormats(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_new", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Create a blank editable document in a new managed UYAP Editor session.")]
    public Task<OpenResult> New(CancellationToken cancellationToken) => Run(() => editor.New(cancellationToken));

    [McpServerTool(Name = "uyap_open_template", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Launch the native Şablon Editörü. Omit path for a new template, or provide an unsigned UDF from the configured input or output root. Use uyap_ribbon with Şablon and uyap_open_form with add-field for template controls.")]
    public Task<OpenResult> OpenTemplate(string? path = null, CancellationToken cancellationToken = default) =>
        Run(() => editor.OpenTemplate(path, cancellationToken));

    [McpServerTool(Name = "uyap_reuse_template_field", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Insert an existing field definition by exact name at a collapsed caret outside fields in the managed native Şablon Editörü.")]
    public Task<EditResult> ReuseTemplateField(string documentId, string fieldName, CancellationToken cancellationToken) =>
        Run(() => editor.ReuseTemplateField(documentId, fieldName, cancellationToken));

    [McpServerTool(Name = "uyap_get_page", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read the editor's actual current page and its total page count.")]
    public Task<PageResult> GetPage(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.Page(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_view_state", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read native zoom, whitespace and comment display settings, caret position, selection and document length. Offsets are UTF-16.")]
    public Task<EditorViewResult> ViewState(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ViewState(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_set_view", UseStructuredContent = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Set a native zoom option or whitespace visibility for one managed editor view, and read back the applied values.")]
    public Task<EditorViewResult> SetView(string documentId, int? zoomPercent = null, bool? showWhitespace = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetView(documentId, zoomPercent, showWhitespace, cancellationToken));

    [McpServerTool(Name = "uyap_set_font", UseStructuredContent = true, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Set the native font family and/or point size for the current nonempty selection in an editable working copy.")]
    public Task<EditorFontResult> SetFont(string documentId, string? family = null, int? size = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetFont(documentId, family, size, cancellationToken));

    [McpServerTool(Name = "uyap_set_palette_color", UseStructuredContent = true, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Apply a #RRGGBB color via the native ribbon palette. Targets are foreground, highlight, paper, or canvas. Text colors need a selection. Paper and canvas update only this managed editor profile.")]
    public Task<PaletteColorResult> SetPaletteColor(string documentId, string target, string color,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetPaletteColor(documentId, target, color, cancellationToken));

    [McpServerTool(Name = "uyap_set_line_spacing", UseStructuredContent = true, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Apply the native paragraph line spacing preset 1, 1.15, 2, 2.5, or 3 at the caret or across the selection.")]
    public Task<LineSpacingResult> SetLineSpacing(string documentId, double multiplier,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetLineSpacing(documentId, multiplier, cancellationToken));

    [McpServerTool(Name = "uyap_apply_format_brush", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Apply UYAP's native format brush from a UTF-16 source character to a nonempty UTF-16 text range. Set includeParagraph to also copy the native paragraph format.")]
    public Task<FormatBrushResult> ApplyFormatBrush(string documentId, int sourceOffset, int start, int end,
        bool includeParagraph = false, CancellationToken cancellationToken = default) =>
        Run(() => editor.ApplyFormatBrush(documentId, sourceOffset, start, end, includeParagraph, cancellationToken));

    [McpServerTool(Name = "uyap_set_list_palette", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Apply a native multilevel list library preset. Choose none, number_parenthesis, number_dot, uppercase, number_outline, or diamond.")]
    public Task<ListPaletteResult> SetListPalette(string documentId, string preset,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetListPalette(documentId, preset, cancellationToken));

    [McpServerTool(Name = "uyap_set_custom_list", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Create a custom list from the selected plain paragraphs, or update the whole custom or numbered list containing the selection. Supply one to six distinct {level, format} entries for levels 1 through 6. Mix number formats and bullets in the same list. Use NUMBER_TYPE_<family>_<suffix> with family NUMBER, CHAR_SMALL, CHAR_BIG, ROMAN_SMALL, or ROMAN_BIG and suffix DOT, TRE, PARANTHESE, or D_PARANTHESE. NUMBER_TYPE_NUMBER_DOT_3 is the decimal outline format. Bullets use BULLET_TYPE_ followed by ELLIPSE, RECTANGLE, RECTANGLE_D, ARROW, DIAMOND, or TRIANGLE. Unspecified levels retain their current format or use decimal with a dot in a new list.")]
    public Task<CustomListResult> SetCustomList(string documentId, ListLevelFormat[] levels,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetCustomList(documentId, levels, cancellationToken));

    [McpServerTool(Name = "uyap_get_list_item", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read the paragraph at the caret or within a single-paragraph selection. Level is null for a plain paragraph. Numbered reflects the current level's marker, including in mixed lists. ExplicitStart is the stored numbering restart. LevelFormats lists stored format overrides. IndentPoints is the listed paragraph's left indent in points.")]
    public Task<ListItemResult> GetListItem(string documentId, CancellationToken cancellationToken = default) =>
        Run(() => editor.GetListItem(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_set_list_level", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Use UYAP's list level action to change the list item at the caret or within a single-paragraph selection. Level is an integer from 1 to 6.")]
    public Task<ListItemResult> SetListLevel(string documentId, int level,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetListLevel(documentId, level, cancellationToken));

    [McpServerTool(Name = "uyap_set_list_indents", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Set the native left indent in points for all items of each specified level in the list at the caret or selection. Each {level, indentPoints} entry targets an existing level from 1 to 6. UYAP positions its number or bullet from the resulting paragraph layout.")]
    public Task<ListIndentsResult> SetListIndents(string documentId, ListLevelIndent[] levels,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetListIndents(documentId, levels, cancellationToken));

    [McpServerTool(Name = "uyap_set_numbering_start", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Restart UYAP numbering at the current numbered list item using a positive startAt. The editor may move following items into the restarted run. Place the caret in one item or select text within that paragraph.")]
    public Task<ListItemResult> SetNumberingStart(string documentId, int startAt,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetNumberingStart(documentId, startAt, cancellationToken));

    [McpServerTool(Name = "uyap_continue_numbering", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Use UYAP's Continue from previous list action on the numbered item at the caret or within a single-paragraph selection. The editor finds the preceding numbered list and may move following items into that list. Requires an editable document.")]
    public Task<ListItemResult> ContinueNumbering(string documentId, CancellationToken cancellationToken = default) =>
        Run(() => editor.ContinueNumbering(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_list_shortcuts", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List exact action names, labels and keyboard accelerators from the active UYAP editor action set.")]
    public Task<EditorShortcutsResult> ListShortcuts(string documentId, int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Shortcuts(documentId, offset, limit, cancellationToken));

    [McpServerTool(Name = "uyap_go_to_page", UseStructuredContent = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Use UYAP's Sayfaya Git command to select a 1-based page and verify the editor's resulting position.")]
    public Task<PageResult> GoToPage(string documentId, int page, CancellationToken cancellationToken) =>
        Run(() => editor.Go(documentId, page, cancellationToken));

    [McpServerTool(Name = "uyap_export_pdf", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Invoke the editor's Dosya > PDF Olarak Kaydet command. Publish a validated PDF to a new local file.")]
    public Task<PdfResult> ExportPdf(string documentId, string outputPath, CancellationToken cancellationToken) =>
        Run(() => editor.ExportPdf(documentId, outputPath, cancellationToken));

    [McpServerTool(Name = "uyap_export_signed_imz", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Export the existing signature data through the editor's Dosya > Dışa Aktar ribbon to a new local IMZ. Does not sign or validate certificate trust.")]
    public Task<SignedImzExport> ExportSignedImz(string documentId, string outputPath, CancellationToken cancellationToken) =>
        Run(() => editor.ExportSignedImz(documentId, outputPath, cancellationToken));

    [McpServerTool(Name = "uyap_encrypt_7z", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Pack one local UDF into a new AES-encrypted 7z archive with encrypted headers. Supply a password file path under the input root. Never include the password in a tool call.")]
    public Task<LocalArchiveResult> Encrypt7z(string udfPath, string outputPath, string passwordFile, CancellationToken cancellationToken) =>
        Run(() => editor.Encrypt7z(udfPath, outputPath, passwordFile, cancellationToken));

    [McpServerTool(Name = "uyap_extract_7z", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Recover a single AES-encrypted UDF entry from a local 7z archive into a new UDF. Supply a password file path under the input root. Use uyap_open to open the recovered file.")]
    public Task<LocalArchiveResult> Extract7z(string archivePath, string outputPath, string passwordFile, CancellationToken cancellationToken) =>
        Run(() => editor.Extract7z(archivePath, outputPath, passwordFile, cancellationToken));

    [McpServerTool(Name = "uyap_save_copy", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Save an editable UDF via the editor's Farklı Kaydet command to a new local file.")]
    public Task<SavedCopy> SaveCopy(string documentId, string outputPath, CancellationToken cancellationToken) =>
        Run(() => editor.SaveCopy(documentId, outputPath, cancellationToken));

    [McpServerTool(Name = "uyap_list_backups", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List UYAP autosave files in managed profiles and the optional explicitly configured backup folder.")]
    public Task<NativeBackups> ListBackups(int offset = 0, int limit = 20, CancellationToken cancellationToken = default) =>
        Run(() => editor.ListBackups(offset, limit, cancellationToken));

    [McpServerTool(Name = "uyap_restore_backup", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Recover a listed UYAP autosave through the native backup panel into a new local UDF. Supply its listed SHA-256.")]
    public Task<BackupRecovery> RestoreBackup(string backupPath, string expectedSha256, string outputPath,
        CancellationToken cancellationToken) =>
        Run(() => editor.RestoreBackup(backupPath, expectedSha256, outputPath, cancellationToken));

    [McpServerTool(Name = "uyap_export_document", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Export using a local editor file filter or native renderer. HTML can include page-break markup. Use uyap_export_pdf for PDF.")]
    public Task<FormatExport> ExportDocument(string documentId, string outputPath, bool htmlPageBreaks = false,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Export(documentId, outputPath, cancellationToken, htmlPageBreaks));

    [McpServerTool(Name = "uyap_batch_export", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Export an explicit batch of UDF files via the native editor. Reports each output and errors independently.")]
    public Task<BatchResult> BatchExport(BatchFile[] files, CancellationToken cancellationToken) =>
        Run(() => editor.Batch(files, cancellationToken));

    [McpServerTool(Name = "uyap_read_text", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read an explicit bounded excerpt of the active UDF. Offsets are UTF-16 code units.")]
    public Task<TextResult> ReadText(string documentId, int start = 0, int limit = 2048,
        CancellationToken cancellationToken = default) => Run(() => editor.ReadText(documentId, start, limit, cancellationToken));

    [McpServerTool(Name = "uyap_page_regions", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read native header and footer text and UTF-16 bounds, including whether each region is present.")]
    public Task<PageRegionsResult> ReadPageRegions(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ReadPageRegions(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_set_page_region", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Create or replace the single-line text of a native header or footer in an editable copy. Region is header or footer.")]
    public Task<PageRegion> SetPageRegion(string documentId, string region, string text,
        CancellationToken cancellationToken) =>
        Run(() => editor.SetPageRegion(documentId, region, text, cancellationToken));

    [McpServerTool(Name = "uyap_page_region_range", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Read or set the native first and last pages of an existing header or footer (1-based). Omit both to read. Set lastPage to -1 to remove the last-page limit.")]
    public Task<PageRegionRange> PageRegionRange(string documentId, string region, int? firstPage = null,
        int? lastPage = null, CancellationToken cancellationToken = default) =>
        Run(() => editor.PageRegionRange(documentId, region, firstPage, lastPage, cancellationToken));

    [McpServerTool(Name = "uyap_select_text", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Select an exact UTF-16 range in the managed UYAP document. Use equal offsets to place the caret.")]
    public Task<SelectionResult> SelectText(string documentId, int start, int end, CancellationToken cancellationToken) =>
        Run(() => editor.SelectText(documentId, start, end, cancellationToken));

    [McpServerTool(Name = "uyap_compare_udf", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Use native UYAP version comparison with a local baseline UDF. Inspect returns a page of changes with changeId values shared by adjacent ranges in a change group. Use nextOffset to continue paging. Optionally set navigationDirection to next or previous during inspect to verify the selected change in the comparison window. Supply navigationFromChangeId to navigate from a known group, or omit it to start at the respective document boundary. Accept_all and reject_all resolve every change. Accept_one applies one group to the baseline and keeps other groups on the baseline side. Reject_one reverts one group in the current version and keeps other groups on the current side. Supply changeId for an individual decision and outputPath for any resolution. Text ranges use UTF-16.")]
    public Task<UdfComparison> CompareUdf(string documentId, string baselinePath, string decision = "inspect",
        string? outputPath = null, int limit = 20, string? changeId = null, int offset = 0,
        string? navigationDirection = null, string? navigationFromChangeId = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.CompareUdf(documentId, baselinePath, decision, outputPath, limit, changeId, offset,
            navigationDirection, navigationFromChangeId, cancellationToken));

    [McpServerTool(Name = "uyap_find", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Find literal text in the active UDF and select the next match. Returns UTF-16 offsets.")]
    public Task<FindResult> Find(string documentId, string query, int start = 0, bool caseSensitive = true,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Find(documentId, query, start, caseSensitive, cancellationToken));

    [McpServerTool(Name = "uyap_spell_check", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Check a bounded part of the active UDF using UYAP's Turkish spelling engine. Returns misspelled words, UTF-16 ranges and suggestions. Use nextStart to continue until complete.")]
    public Task<SpellingResult> SpellCheck(string documentId, int start = 0, int characterLimit = 2048, int issueLimit = 20,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SpellCheck(documentId, start, characterLimit, issueLimit, cancellationToken));

    [McpServerTool(Name = "uyap_apply_spelling", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Apply a current UYAP spelling suggestion to an editable copy. The exact UTF-16 range and expected word must still match.")]
    public Task<EditResult> ApplySpelling(string documentId, int start, int end, string expectedWord, string suggestion,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.ApplySpelling(documentId, start, end, expectedWord, suggestion, cancellationToken));

    [McpServerTool(Name = "uyap_replace_text", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Replace a UTF-16 range in an editable working copy, or insert when start equals end.")]
    public Task<EditResult> ReplaceText(string documentId, int start, int end, string text, CancellationToken cancellationToken) =>
        Run(() => editor.Replace(documentId, start, end, text, cancellationToken));

    [McpServerTool(Name = "uyap_list_automatic_text", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List native plain and rich automatic-text definitions and managed text shortcuts in this document's isolated UYAP profile.")]
    public Task<AutomaticTexts> ListAutomaticText(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.ListAutomaticText(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_set_automatic_text", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Set or delete an automatic-text key in the managed profile. Supply text for plain or richSourcePath to a local unsigned UDF for rich and shortcut. For a shortcut the key is a Swing key stroke such as 'ctrl shift pressed F12'.")]
    public Task<AutomaticTexts> SetAutomaticText(string documentId, string key, string kind = "plain",
        string operation = "set", string? text = null, string? richSourcePath = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.SetAutomaticText(documentId, key, kind, operation, text, richSourcePath, cancellationToken));

    [McpServerTool(Name = "uyap_insert_automatic_text", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Use the native UYAP automatic-text definition to replace the current editable selection, or insert at the caret.")]
    public Task<AutomaticTextInsertion> InsertAutomaticText(string documentId, string key, string kind = "plain",
        CancellationToken cancellationToken = default) =>
        Run(() => editor.InsertAutomaticText(documentId, key, kind, cancellationToken));

    [McpServerTool(Name = "uyap_list_comments", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List native UYAP comments and their UTF-16 ranges in a managed document. Limit must be 1 through 20.")]
    public Task<CommentsResult> ListComments(string documentId, int offset = 0, int limit = 20,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.ListComments(documentId, offset, limit, cancellationToken));

    [McpServerTool(Name = "uyap_add_comment", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Add a native UYAP comment with text to a nonempty UTF-16 range in an editable working copy. Native type is 1 or 2.")]
    public Task<EditorComment> AddComment(string documentId, int start, int end, string text, int type = 1,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.AddComment(documentId, start, end, text, type, cancellationToken));

    [McpServerTool(Name = "uyap_edit_comment", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Replace native UYAP comment text in an editable working copy. Supply the index, range and text from the latest comment listing.")]
    public Task<EditorComment> EditComment(string documentId, int index, int expectedStart, int expectedEnd,
        string expectedText, string text, CancellationToken cancellationToken) =>
        Run(() => editor.EditComment(documentId, index, expectedStart, expectedEnd, expectedText, text, cancellationToken));

    [McpServerTool(Name = "uyap_change_comment", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Delete a native UYAP comment or toggle whether it is visible in print. Use operation delete or toggle_type and supply the latest index, UTF-16 range, type and text from uyap_list_comments.")]
    public Task<CommentChangeResult> ChangeComment(string documentId, int index, int expectedStart, int expectedEnd,
        int expectedType, string expectedText, string operation, CancellationToken cancellationToken) =>
        Run(() => editor.ChangeComment(documentId, index, expectedStart, expectedEnd, expectedType, expectedText,
            operation, cancellationToken));

    [McpServerTool(Name = "uyap_insert_image", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Choose an authorized local image for an editable copy using the native insert-image or page background action.")]
    public Task<ImageResult> InsertImage(string documentId, string path, bool background = false,
        CancellationToken cancellationToken = default) => Run(() => editor.InsertImage(documentId, path, background, cancellationToken));

    [McpServerTool(Name = "uyap_list_actions", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List the live Swing action IDs of the current editor. Discovery includes actions that require user interaction.")]
    public Task<ActionsResult> ListActions(string documentId, int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default) => Run(() => editor.Actions(documentId, offset, limit, cancellationToken));

    [McpServerTool(Name = "uyap_menu_inventory", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List native menu paths and availability or Swing action IDs and their labels. Use scope menus or actions and page using offset and limit.")]
    public Task<CatalogResult> Catalog(string documentId, string scope = "menus", int offset = 0, int limit = 50,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Catalog(documentId, scope, offset, limit, cancellationToken));

    [McpServerTool(Name = "uyap_list_windows", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("List this MCP server's open UYAP document and template windows and their document IDs.")]
    public Task<ManagedWindows> ListWindows(CancellationToken cancellationToken = default) =>
        Run(() => editor.Windows(cancellationToken));

    [McpServerTool(Name = "uyap_ribbon", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Discover the current UYAP ribbon tabs. Provide a tab name to inspect its live command labels and enabled states.")]
    public Task<RibbonResult> Ribbon(string documentId, string? tab = null, CancellationToken cancellationToken = default) =>
        Run(() => editor.Ribbon(documentId, tab, cancellationToken));

    [McpServerTool(Name = "uyap_run_action", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Invoke a supported local selection, view, editing or formatting action by exact action ID on the managed UDF. For default-typed supply one printable character as text. For insert-content supply the text to insert. For edit-hf, supply replacement text for the existing header or, if absent, footer. application-freemem-level1 clears this session's undo/redo history and isolated clipboard.")]
    public Task<ActionResult> RunAction(string documentId, string action, string? text = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.Action(documentId, action, text, cancellationToken));

    [McpServerTool(Name = "uyap_run_macro", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Record one to eight native formatting or case actions on the first selection, stop recording, then replay on a separate selection in the same editable document. All offsets are UTF-16. Supported IDs are font-bold, font-italic, font-underline, left-justify, right-justify, center-justify, both-justify, changeCase-upper and changeCase-lower.")]
    public Task<MacroResult> RunMacro(string documentId, string[] actions, int recordStart, int recordEnd,
        int replayStart, int replayEnd, CancellationToken cancellationToken) =>
        Run(() => editor.Macro(documentId, actions, recordStart, recordEnd, replayStart, replayEnd, cancellationToken));

    [McpServerTool(Name = "uyap_open_form", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Open a supported local editor action dialog, such as table insertion, paragraph formatting, page setup, comments or viewing signatures. Returns a revision and actionable controls.")]
    public Task<DialogResult> OpenForm(string documentId, string action, CancellationToken cancellationToken) =>
        Run(() => editor.OpenForm(documentId, action, cancellationToken));

    [McpServerTool(Name = "uyap_inspect_form", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("Read the managed local editor dialog, current control values and a fresh revision for the next action.")]
    public Task<DialogResult> InspectForm(string documentId, CancellationToken cancellationToken) =>
        Run(() => editor.InspectForm(documentId, cancellationToken));

    [McpServerTool(Name = "uyap_form_input", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Apply write, choose, toggle, press, cancel, or color (#RRGGBB on native color settings) to a managed local editor dialog. Use the latest revision and control ID.")]
    public Task<DialogResult> FormInput(string documentId, string revision, int controlId, string operation,
        string value = "", CancellationToken cancellationToken = default) =>
        Run(() => editor.FormInput(documentId, revision, controlId, operation, value, cancellationToken));

    [McpServerTool(Name = "uyap_prepare_workflow", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Select the editor tab for a native user workflow and report command availability. Supports signing, certificates, physical printing, sending, portal save, recovery, signed export, update, proxy settings, and external help.")]
    public Task<ManualWorkflow> PrepareWorkflow(string documentId, string workflow, CancellationToken cancellationToken) =>
        Run(() => editor.Handoff(documentId, workflow, cancellationToken));

    [McpServerTool(Name = "uyap_prepare_attachment", UseStructuredContent = true, Destructive = true, OpenWorld = false)]
    [Description("Prepare a local email attachment from an open managed document. Select attachto-mail (UDF), attachto-mail-doc (DOC), attachto-mail-odt (ODT), or attachto-mail-pdf (PDF) and provide a new output path with the matching extension. The resulting file is ready for the user to attach.")]
    public Task<AttachmentPreparation> PrepareAttachment(string documentId, string action, string outputPath,
        CancellationToken cancellationToken) =>
        Run(() => editor.PrepareAttachment(documentId, action, outputPath, cancellationToken));

    [McpServerTool(Name = "uyap_prepare_approval", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Prepare local review artifacts and list the exact manual confirmation step for a supported UYAP action. Pass an ordered list of managed documents and the action-specific settings. The result is awaiting_user_confirmation, input_required, or blocked_prerequisite.")]
    public Task<ApprovalReview> PrepareApproval(ApprovalRequest request, CancellationToken cancellationToken = default) =>
        Run(() => editor.PrepareApproval(request, cancellationToken));

    [McpServerTool(Name = "uyap_edit_mode", UseStructuredContent = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Read or set the managed document edit mode. Set editable=false to make the native pane and MCP read-only, or editable=true to restore editing in a session originally opened with editing authorization. Omit editable to inspect without changing the mode.")]
    public Task<EditModeResult> EditMode(string documentId, bool? editable = null,
        CancellationToken cancellationToken = default) =>
        Run(() => editor.EditMode(documentId, editable, cancellationToken));

    [McpServerTool(Name = "uyap_close", UseStructuredContent = true, Destructive = false, OpenWorld = false)]
    [Description("Close a managed UYAP document if it can be closed without discarding unsaved edits.")]
    public Task<CloseResult> Close(string documentId, CancellationToken cancellationToken) => Run(() => editor.Close(documentId, cancellationToken));

    private static async Task<T> Run<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (EditorFailure error) { throw ToMcpError(error); }
        catch (UnauthorizedAccessException) { throw ToMcpError(new EditorFailure(EditorErrors.AccessDenied)); }
        catch (IOException) { throw ToMcpError(new EditorFailure(EditorErrors.LocalIoError)); }
    }

    private static McpException ToMcpError(EditorFailure error) => new($"{error.Code}: {error.Message}");
}
