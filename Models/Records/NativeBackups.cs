namespace UyapEditorMcp;

public sealed record NativeBackups(int Offset, NativeBackup[] Backups, int? NextOffset);
