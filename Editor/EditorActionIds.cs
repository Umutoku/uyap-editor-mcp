namespace UyapEditorMcp;

internal static class EditorActionIds
{
    internal const string AttachUdf = "attachto-mail";
    internal const string AttachDoc = "attachto-mail-doc";
    internal const string AttachOdt = "attachto-mail-odt";
    internal const string AttachPdf = "attachto-mail-pdf";
    internal const string PrintDocument = "document-print";
    internal const string PrintBatch = "print-batch";
    internal const string ValidateCertificate = "certificate-owned-validate";
    internal const string ExportSignedDocument = "document-export-signedddoc";
    internal const string SignMobile = "Mobile-Sign";
    internal const string SelectSigningLibrary = "Set Sign Libary";
    internal const string Sign = "sign";
    internal const string SignBatch = "sign-batch";
    internal const string SaveToPortal = "dss-save";
    internal const string SetProxy = "set proxy";
    internal const string OpenHelp = "set-tooltip-enable";
    internal const string Update = "update";

    internal const string SelectionBegin = "selection-begin";
    internal const string SelectionEnd = "selection-end";
    internal const string CaretBegin = "caret-begin";
    internal const string CaretEnd = "caret-end";
    internal const string SelectAll = "select-all";
    internal const string ShowWhitespace = "view-showWhiteSpace";
    internal const string Fullscreen = "fullscreen";
    internal const string ResetFullscreen = "resetfullscreen";
    internal const string CaretBeginLine = "caret-begin-line";
    internal const string CaretEndLine = "caret-end-line";
    internal const string CaretPreviousWord = "caret-previous-word";
    internal const string CaretNextWord = "caret-next-word";
    internal const string CaretBeginWord = "caret-begin-word";
    internal const string CaretEndWord = "caret-end-word";
    internal const string SelectLine = "select-line";
    internal const string SelectWord = "select-word";
    internal const string SelectionBeginLine = "selection-begin-line";
    internal const string SelectionEndLine = "selection-end-line";
    internal const string SelectionPreviousWord = "selection-previous-word";
    internal const string SelectionNextWord = "selection-next-word";
    internal const string SelectionBeginWord = "selection-begin-word";
    internal const string SelectionEndWord = "selection-end-word";
    internal const string PageUp = "page-up";
    internal const string PageDown = "page-down";
    internal const string SelectionPageUp = "selection-page-up";
    internal const string SelectionPageDown = "selection-page-down";
    internal const string CopyToClipboard = "copy-to-clipboard";
    internal const string CommentNext = "comment-next";
    internal const string CommentPrevious = "comment-previous";
    internal const string CommentShow = "comment-show";
    internal const string CaretBackward = "caret-backward";
    internal const string CaretForward = "caret-forward";
    internal const string CaretDown = "caret-down";
    internal const string CaretUp = "caret-up";
    internal const string CaretBeginParagraph = "caret-begin-paragraph";
    internal const string CaretEndParagraph = "caret-end-paragraph";
    internal const string SelectParagraph = "select-paragraph";
    internal const string SelectionBackward = "selection-backward";
    internal const string SelectionForward = "selection-forward";
    internal const string SelectionDown = "selection-down";
    internal const string SelectionUp = "selection-up";
    internal const string SelectionBeginParagraph = "selection-begin-paragraph";
    internal const string SelectionEndParagraph = "selection-end-paragraph";
    internal const string SelectionPageLeft = "selection-page-left";
    internal const string SelectionPageRight = "selection-page-right";
    internal const string Unselect = "unselect";
    internal const string WindowsMenu = "windows-menu";
    internal const string Beep = "beep";
    internal const string DebugLayoutMetrics = "debug-lvuMetrics";
    internal const string DumpModel = "dump-model";
    internal const string ShowParagraphProperties = "show-paragraph-properties";
    internal const string InfoSelection = "info-selection";
    internal const string SelectionWordInfo = "selection-word-info";
    internal const string AllShortcuts = "all-shortcuts";
    internal const string ShowSignatures = "show-signatures";
    internal const string PrintPreview = "print-preview";
    internal const string ShowAllAnnotation = "show-all-annotation";
    internal const string ReleaseNotesActionStr = "release-notes-action-str";
    internal const string ShowElementTreePanel = "show-elementTreePanel";
    internal const string LoggerActivator = "logger-activator";
    internal const string DefaultTyped = "default-typed";
    internal const string InsertContent = "insert-content";
    internal const string EditHeaderFooter = "edit-hf";
}

internal static class EditorWorkflowIds
{
    internal const string Sign = EditorActionIds.Sign;
    internal const string SignMobile = "mobile_sign";
    internal const string SignBatch = "batch_sign";
    internal const string Print = "print";
    internal const string PrintBatch = "batch_print";
    internal const string Send = "send";
    internal const string SaveToPortal = "portal_save";
    internal const string Recover = "recover";
    internal const string Certificates = "certificates";
    internal const string ExportSigned = "signed_export";
    internal const string Update = EditorActionIds.Update;
    internal const string Proxy = "proxy";
    internal const string Help = "help";
    internal const string SigningLibrary = "signing_library";
}

internal static class EditorWorkflowStatuses
{
    internal const string AwaitingUserConfirmation = "awaiting_user_confirmation";
    internal const string BlockedPrerequisite = "blocked_prerequisite";
    internal const string InputRequired = "input_required";
    internal const string UserActionRequired = "user_action_required";
    internal const string UnavailableInCurrentMode = "unavailable_in_current_mode";
}
