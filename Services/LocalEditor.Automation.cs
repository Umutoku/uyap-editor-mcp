namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    private static readonly HashSet<string> ViewActions =
        new([
            EditorActionIds.SelectionBegin, EditorActionIds.SelectionEnd, EditorActionIds.CaretBegin,
            EditorActionIds.CaretEnd, EditorActionIds.SelectAll, EditorActionIds.ShowWhitespace,
            EditorActionIds.Fullscreen, EditorActionIds.ResetFullscreen, EditorActionIds.CaretBeginLine,
            EditorActionIds.CaretEndLine, EditorActionIds.CaretPreviousWord, EditorActionIds.CaretNextWord,
            EditorActionIds.CaretBeginWord, EditorActionIds.CaretEndWord, EditorActionIds.SelectLine,
            EditorActionIds.SelectWord, EditorActionIds.SelectionBeginLine, EditorActionIds.SelectionEndLine,
            EditorActionIds.SelectionPreviousWord, EditorActionIds.SelectionNextWord, EditorActionIds.SelectionBeginWord,
            EditorActionIds.SelectionEndWord, EditorActionIds.PageUp, EditorActionIds.PageDown,
            EditorActionIds.SelectionPageUp, EditorActionIds.SelectionPageDown, EditorActionIds.CopyToClipboard,
            EditorActionIds.CommentNext, EditorActionIds.CommentPrevious, EditorActionIds.CommentShow,
            EditorActionIds.CaretBackward, EditorActionIds.CaretForward, EditorActionIds.CaretDown,
            EditorActionIds.CaretUp, EditorActionIds.CaretBeginParagraph, EditorActionIds.CaretEndParagraph,
            EditorActionIds.SelectParagraph, EditorActionIds.SelectionBackward, EditorActionIds.SelectionForward,
            EditorActionIds.SelectionDown, EditorActionIds.SelectionUp, EditorActionIds.SelectionBeginParagraph,
            EditorActionIds.SelectionEndParagraph, EditorActionIds.SelectionPageLeft, EditorActionIds.SelectionPageRight,
            EditorActionIds.Unselect, EditorActionIds.WindowsMenu, EditorActionIds.Beep,
            EditorActionIds.DebugLayoutMetrics, EditorActionIds.DumpModel, EditorActionIds.ShowParagraphProperties
        ],
            StringComparer.Ordinal);
    private static readonly HashSet<string> ReadOnlyForms =
        new([
            EditorActionIds.SelectSigningLibrary, EditorActionIds.InfoSelection, EditorActionIds.SelectionWordInfo,
            EditorActionIds.AllShortcuts, EditorActionIds.ShowSignatures, EditorActionIds.PrintPreview,
            EditorActionIds.ShowAllAnnotation, EditorActionIds.ReleaseNotesActionStr, EditorActionIds.ShowElementTreePanel,
            EditorActionIds.LoggerActivator
        ], StringComparer.Ordinal);

    internal Task<ActionResult> Action(string id, string action, string? text, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        if (!ViewActions.Contains(action))
            RequireEditable(session);
        bool takesText = action is EditorActionIds.DefaultTyped or EditorActionIds.InsertContent or EditorActionIds.EditHeaderFooter;
        if (takesText ? text == null || text.Length > 8192 || text.Contains('\0') ||
                (text.Length == 0 && action != EditorActionIds.EditHeaderFooter) : text != null)
            throw new EditorException(EditorErrors.ActionTextInvalid);
        if (action == EditorActionIds.DefaultTyped && (text!.EnumerateRunes().Count() != 1 || char.IsControl(text[0])))
            throw new EditorException(EditorErrors.TypedCharacterInvalid);
        var result = await session.Editor.Call(EditorBridgeCommands.Action, token, action, text ?? "");
        return new ActionResult(action, ReadNumber(result[0]), ReadNumber(result[1]), result.ElementAtOrDefault(2));
    }, token);

    internal Task<MacroResult> Macro(string id, string[] actions, int recordStart, int recordEnd,
        int replayStart, int replayEnd, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        if (actions.Length is < 1 or > 8)
            throw new EditorException(EditorErrors.MacroStepsInvalid);
        string[] arguments = [WriteNumber(recordStart), WriteNumber(recordEnd), WriteNumber(replayStart),
            WriteNumber(replayEnd), .. actions];
        var result = await session.Editor.Call(EditorBridgeCommands.Macro, token, arguments);
        return new MacroResult(ReadNumber(result[0]), ReadNumber(result[1]), ReadNumber(result[2]));
    }, token);

    internal Task<DialogResult> OpenForm(string id, string action, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        if (!ReadOnlyForms.Contains(action))
            RequireEditable(session);
        return ParseDialog(await session.Editor.Call(EditorBridgeCommands.OpenForm, token, action));
    }, token);

    internal Task<DialogResult> InspectForm(string id, CancellationToken token) => Exclusive(async () =>
        ParseDialog(await Get(id).Editor.Call(EditorBridgeCommands.InspectForm, token)), token);

    internal Task<DialogResult> FormInput(string id, string revision, int control, string operation, string value,
        CancellationToken token) => Exclusive(async () =>
    {
        if (value.Length > 8192)
            throw new EditorException(EditorErrors.TextLimit);
        var session = Get(id);
        if (!session.Editable && operation != "cancel")
            throw new EditorException(EditorErrors.ReadOnlyForm);
        return ParseDialog(await session.Editor.Call(EditorBridgeCommands.FormInput, token, revision, WriteNumber(control), operation, value));
    }, token);

    private static DialogResult ParseDialog(string[] response)
    {
        if (response[0] != "open")
            return new DialogResult(response[0], null, null, []);
        var items = response.Skip(3).Chunk(5).Select(control => new DialogControl(
            ReadNumber(control[0]), control[1], control[2], control[3], control[4].Length == 0 ? [] : control[4].Split('\u001f')));
        return new DialogResult(response[0], response[1], response[2], items.ToArray());
    }

    internal Task<ActionsResult> Actions(string id, int offset, int limit, CancellationToken token) => Exclusive(async () =>
    {
        if (offset < 0 || limit is < 1 or > 100)
            throw new EditorException(EditorErrors.RangeInvalid);
        var session = Get(id);
        var actions = await session.Editor.Call(EditorBridgeCommands.Actions, token);
        var sorted = actions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new ActionsResult(sorted.Length, offset, sorted.Skip(offset).Take(limit).ToArray(), "Swing editor actions");
    }, token);

    internal Task<CatalogResult> Catalog(string id, string scope, int offset, int limit, CancellationToken token) =>
        Exclusive(async () =>
        {
            if (scope is not ("menus" or "actions"))
                throw new EditorException(EditorErrors.InventoryScopeInvalid);
            if (offset < 0 || limit is < 1 or > 100)
                throw new EditorException(EditorErrors.RangeInvalid);
            var session = Get(id);
            var values = await session.Editor.Call(EditorBridgeCommands.Catalog, token, scope);
            if (values.Length % 5 != 0)
                throw new EditorException(EditorErrors.MenuCatalogResponseInvalid);
            var entries = values.Chunk(5).Select(row => new CatalogEntry(
                row[0].Split('\u001f', StringSplitOptions.RemoveEmptyEntries), row[1], row[2],
                row[3].Length == 0 ? null : row[3], row[4]));
            return new CatalogResult(session.TemplateMode ? "template" : "document", scope, values.Length / 5, offset,
                entries.Skip(offset).Take(limit).ToArray());
        }, token);

    internal Task<ManagedWindows> Windows(CancellationToken token) => Exclusive(() =>
    {
        var open = sessions.Values.Where(session => !session.Editor.Exited)
            .Select(session => new ManagedWindow(session.Id, Path.GetFileName(session.Staged),
                session.TemplateMode ? "template" : "document", session.Editable));
        return Task.FromResult(new ManagedWindows(open.ToArray()));
    }, token);

    internal Task<RibbonResult> Ribbon(string id, string? tab, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        var tabs = await session.Editor.Call(EditorBridgeCommands.Tabs, token);
        if (tab == null)
            return new RibbonResult(tabs, null, []);
        var entries = await session.Editor.Call(EditorBridgeCommands.Commands, token, tab);
        var buttons = entries.Chunk(2)
            .Where(entry => entry.Length == 2 && !string.IsNullOrWhiteSpace(entry[0]))
            .Select(entry => new RibbonButton(entry[0].Trim(), entry[1] == "true"))
            .Distinct().ToArray();
        return new RibbonResult(tabs, tab, buttons);
    }, token);

    internal async Task<ManualWorkflow> Handoff(string id, string workflow, CancellationToken token)
    {
        var (tab, command) = workflow switch
        {
            EditorWorkflowIds.Sign => (EditorMenuLabels.Tools, "İmzala"),
            EditorWorkflowIds.SignMobile => (EditorMenuLabels.Tools, "Mobil İmzala"),
            EditorWorkflowIds.SignBatch => (EditorMenuLabels.File, "Toplu İmzala"),
            EditorWorkflowIds.Print => (EditorMenuLabels.File, "Yazdır"),
            EditorWorkflowIds.PrintBatch => (EditorMenuLabels.File, "Toplu Yazdır"),
            EditorWorkflowIds.Send => (EditorMenuLabels.File, "Gönder"),
            EditorWorkflowIds.SaveToPortal => (EditorMenuLabels.File, "Sisteme Kaydet"),
            EditorWorkflowIds.Recover => (EditorMenuLabels.File, "Yedekleri Aç"),
            EditorWorkflowIds.Certificates => (EditorMenuLabels.Tools, "Sertifikalar"),
            EditorWorkflowIds.ExportSigned => (EditorMenuLabels.File, EditorMenuLabels.SignedExport),
            EditorWorkflowIds.Update => (EditorMenuLabels.Tools, "Editorü Güncelle"),
            EditorWorkflowIds.Proxy => (EditorMenuLabels.Tools, "Proxy"),
            EditorWorkflowIds.Help => (EditorMenuLabels.View, "Yardım Özelliklerini Aç"),
            _ => throw new EditorException(EditorErrors.WorkflowUnknown),
        };
        var ribbon = await Ribbon(id, tab, token);
        var enabled = ribbon.Commands.Any(button => button.Enabled &&
            (button.Label == command || (workflow == EditorWorkflowIds.SignMobile && button.Label == "Dokümanı Mobil İmzala")));
        return new ManualWorkflow(workflow, tab, command, enabled, enabled ? EditorWorkflowStatuses.UserActionRequired : EditorWorkflowStatuses.UnavailableInCurrentMode);
    }
}
