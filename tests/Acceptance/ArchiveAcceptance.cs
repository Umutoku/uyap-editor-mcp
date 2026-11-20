using static UyapEditorMcp.Acceptance.AcceptanceClient;

namespace UyapEditorMcp.Acceptance;

internal static class ArchiveAcceptance
{
    internal static async Task Run(AcceptanceClient client, string source, string input, string output)
    {
        var password = Path.Combine(input, "synthetic-password.txt");
        var wrongPassword = Path.Combine(input, "synthetic-wrong-password.txt");
        File.WriteAllText(password, "Kurgu Şİfre " + Guid.NewGuid());
        File.WriteAllText(wrongPassword, Guid.NewGuid().ToString());
        var archive = Path.Combine(output, "synthetic.7z");
        var restored = Path.Combine(output, "restored.udf");
        await client.Call(McpToolNames.Encrypt7z, ("udfPath", source), ("outputPath", archive),
            ("passwordFile", password));
        await client.ExpectError(McpToolNames.Extract7z, "SEVEN_ZIP_FAILED", ("archivePath", archive),
            ("outputPath", restored), ("passwordFile", wrongPassword));
        Require(!File.Exists(restored), "Wrong password produced output.");
        await client.Call(McpToolNames.Extract7z, ("archivePath", archive), ("outputPath", restored),
            ("passwordFile", password));
        Require(File.ReadAllBytes(source).AsSpan().SequenceEqual(File.ReadAllBytes(restored)), "7z changed the UDF.");
        var opened = await client.Call(McpToolNames.Open, ("path", restored));
        var id = opened.GetProperty("documentId").GetString();
        var page = await client.Call(McpToolNames.GoToPage, ("documentId", id), ("page", 3));
        Require(page.GetProperty("page").GetInt32() == 3, "The recovered UDF did not reopen at the expected page.");
        await client.Call(McpToolNames.Close, ("documentId", id));
    }
}
