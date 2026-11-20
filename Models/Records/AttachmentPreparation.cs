namespace UyapEditorMcp;

public sealed record AttachmentPreparation(string Action, string Format, string Path, long Bytes, string Sha256,
    string EditorCommand, string Status);
