namespace UyapEditorMcp;

public sealed record StatusResult(string EditorVersion, string Platform, int ManagedDocuments, string Bridge, string[] Capabilities);
