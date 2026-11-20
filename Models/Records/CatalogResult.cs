namespace UyapEditorMcp;

public sealed record CatalogResult(string Mode, string Scope, int Total, int Offset, CatalogEntry[] Entries);
