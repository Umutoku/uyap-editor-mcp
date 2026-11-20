namespace UyapEditorMcp;

public sealed class BatchFile(string path, string outputPath)
{
    public string Path { get; } = path;
    public string OutputPath { get; } = outputPath;
}
