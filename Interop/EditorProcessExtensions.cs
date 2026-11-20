namespace UyapEditorMcp;

internal static class EditorProcessExtensions
{
    internal static async Task<bool> HasSourceChanges(
        this EditorProcess editor,
        EditorError invalidResponse,
        CancellationToken cancellationToken)
    {
        var response = await editor.Call(EditorBridgeCommands.PdfSourceChanged, cancellationToken);

        return response switch
        {
            [EditorWireValues.True] => true,
            [EditorWireValues.False] => false,
            _ => throw new EditorFailure(invalidResponse),
        };
    }
}
