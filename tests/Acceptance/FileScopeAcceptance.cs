using System.Security.Cryptography;
using static UyapEditorMcp.Acceptance.AcceptanceClient;

namespace UyapEditorMcp.Acceptance;

internal static class FileScopeAcceptance
{
    internal static async Task Run(AcceptanceClient client, string source, string input, string output, string backups)
    {
        var subfolder = Directory.CreateDirectory(Path.Combine(output, "nested")).FullName;
        var rtf = CopyFixture("sentetik-yerel.rtf", input);
        var imported = await client.Call(McpToolNames.ImportDocument, ("path", rtf));
        await client.Call(McpToolNames.Close, ("documentId", imported.GetProperty("documentId").GetString()));

        var opened = await client.Call(McpToolNames.Open, ("path", source), ("editable", true));
        var id = opened.GetProperty("documentId").GetString();
        var compared = await client.Call(McpToolNames.CompareUdf, ("documentId", id), ("baselinePath", source));
        Require(compared.GetProperty("total").GetInt32() == 0, "Native comparison changed an unchanged document.");

        var autoText = await client.Call(McpToolNames.SetAutomaticText, ("documentId", id), ("key", "scopetest"),
            ("kind", "rich"), ("richSourcePath", source));
        Require(autoText.GetProperty("entries").EnumerateArray().Any(entry =>
            entry.GetProperty("key").GetString() == "scopetest"), "The rich text source was not staged.");

        var svg = Path.Combine(subfolder, "synthetic.svg");
        await client.Call(McpToolNames.ExportDocument, ("documentId", id), ("outputPath", svg));
        Require(new FileInfo(svg).Length > 0, "The nested native export is empty.");

        var image = CopyFixture("sentetik-gorsel.png", input);
        await client.Call(McpToolNames.InsertImage, ("documentId", id), ("path", image));
        await client.Call(McpToolNames.SaveCopy, ("documentId", id), ("outputPath", Path.Combine(subfolder, "with-image.udf")));
        await client.Call(McpToolNames.Close, ("documentId", id));

        var backup = Path.Combine(backups, "~synthetic.udf");
        File.Copy(source, backup);
        var bytes = File.ReadAllBytes(backup);
        var restored = Path.Combine(subfolder, "recovered.udf");
        await client.Call(McpToolNames.RestoreBackup, ("backupPath", backup),
            ("expectedSha256", Convert.ToHexString(SHA256.HashData(bytes))), ("outputPath", restored));
        Require(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(restored)), "Native recovery changed the backup.");
    }

    private static string CopyFixture(string file, string input)
    {
        var target = Path.Combine(input, file);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", file), target);
        return target;
    }
}
