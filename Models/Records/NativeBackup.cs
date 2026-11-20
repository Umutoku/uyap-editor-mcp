namespace UyapEditorMcp;

public sealed record NativeBackup(string Path, string Name, long Bytes, DateTime ModifiedUtc, string Sha256);
