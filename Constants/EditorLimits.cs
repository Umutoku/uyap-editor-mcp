namespace UyapEditorMcp;

internal static class EditorLimits
{
    private const int Mebibyte = 1024 * 1024;

    internal const int DocumentBytes = 50 * Mebibyte;
    internal const int ArchiveBytes = 51 * Mebibyte;
    internal const int ImageBytes = 15 * Mebibyte;
    internal const int ArchiveListingBytes = 128 * 1024;
    internal const int ArchivePasswordFileBytes = 1024;
    internal const int MaxXmlCharacters = 50 * 1024 * 1024;
    internal const int ListLevels = 6;
}
