namespace UyapEditorMcp;

public sealed record BatchItem(string FileName, bool Succeeded, string? OutputPath, string? Error, string? OpenDocumentId);
