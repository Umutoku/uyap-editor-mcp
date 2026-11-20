namespace UyapEditorMcp;

public sealed record PdfResult(string Path, int PageCount, long Bytes, string Sha256, string EditorCommand, bool SourceUnchanged);
