namespace UyapEditorMcp;

public sealed record OpenResult(string DocumentId, string FileName, string Version, bool Editable, string? InputSha256, PageResult? Position,
    string Mode = "document", string? SignatureStatus = null);
