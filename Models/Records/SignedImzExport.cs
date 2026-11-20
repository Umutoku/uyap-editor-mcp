namespace UyapEditorMcp;

public sealed record SignedImzExport(string Path, long Bytes, string Sha256, string EditorCommand, bool SourceUnchanged);
