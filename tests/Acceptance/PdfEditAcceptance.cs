using System.Text.Json;
using UglyToad.PdfPig;
using static UyapEditorMcp.Acceptance.AcceptanceClient;

namespace UyapEditorMcp.Acceptance;

internal static class PdfEditAcceptance
{
    private const string Marker = "MCPPDFEDITCHECK";
    private const string Undo = "undo";

    internal static async Task Run(AcceptanceClient client, string id, string output)
    {
        await client.Call(McpToolNames.ReplaceText, ("documentId", id), ("start", 0), ("end", 0), ("text", Marker + " "));
        var edited = await CheckPdf(client, id, Path.Combine(output, "unsaved-edit.pdf"), expectMarker: true);
        await client.Call(McpToolNames.RunAction, ("documentId", id), ("action", Undo));
        var undone = await CheckPdf(client, id, Path.Combine(output, "after-undo.pdf"), expectMarker: false);
        await client.ExpectError(McpToolNames.Close, "UNSAVED_CHANGES", ("documentId", id));
        var saved = Path.Combine(output, "after-undo.udf");
        await client.Call(McpToolNames.SaveCopy, ("documentId", id), ("outputPath", saved));
        Require(File.Exists(saved), "The restored state was not saved as a UDF copy.");
        Console.WriteLine(JsonSerializer.Serialize(new { editedPdfCommand = edited, undonePdfCommand = undone }));
    }

    private static async Task<string> CheckPdf(AcceptanceClient client, string id, string path, bool expectMarker)
    {
        var result = await client.Call(McpToolNames.ExportPdf, ("documentId", id), ("outputPath", path));
        using var pdf = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = false });
        var containsMarker = pdf.GetPages().Any(page => page.Text.Contains(Marker, StringComparison.Ordinal));
        Require(pdf.NumberOfPages == 3 && result.GetProperty("sourceUnchanged").GetBoolean() && containsMarker == expectMarker,
            expectMarker ? "The unsaved edit is missing from the PDF." : "The undone edit is still in the PDF.");
        return result.GetProperty("editorCommand").GetString()!;
    }
}
