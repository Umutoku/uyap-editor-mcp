using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using UyapEditorMcp;
using UyapEditorMcp.Acceptance;
using static UyapEditorMcp.Acceptance.AcceptanceClient;

if (args.Length is < 4 or > 5)
{
    Console.Error.WriteLine("Usage: Acceptance <mcp.exe> <editor-directory> <java-bin> <synthetic-udf> [synthetic-signed-udf]");
    return 2;
}

var scratch = Directory.CreateTempSubdirectory("uyap-mcp-acceptance-");
try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    var input = scratch.CreateSubdirectory("input");
    var output = scratch.CreateSubdirectory("output");
    var state = scratch.CreateSubdirectory("state");
    var backups = scratch.CreateSubdirectory("backups");
    var source = Path.Combine(input.FullName, "synthetic.udf");
    File.Copy(Path.GetFullPath(args[3]), source);
    string? signedSource = null;
    if (args.Length == 5)
    {
        signedSource = Path.Combine(input.FullName, "synthetic-signed.udf");
        File.Copy(Path.GetFullPath(args[4]), signedSource);
    }
    var original = File.ReadAllBytes(source);
    var start = new ProcessStartInfo(Path.GetFullPath(args[0]))
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        ArgumentList = { "--editor-dir", Path.GetFullPath(args[1]), "--java-bin", Path.GetFullPath(args[2]),
            "--input-root", input.FullName, "--output-root", output.FullName, "--state-dir", state.FullName,
            "--backup-root", backups.FullName },
    };
    using var server = Process.Start(start) ?? throw new InvalidOperationException("The acceptance server did not start.");
    var transport = new StreamClientTransport(server.StandardInput.BaseStream, server.StandardOutput.BaseStream);
    try
    {
        return await Run(transport, timeout.Token, source, original, input.FullName, output.FullName, signedSource, backups.FullName);
    }
    finally
    {
        if (!server.WaitForExit(5000))
        {
            server.Kill(entireProcessTree: true);
            server.WaitForExit();
        }
    }
}
finally
{
    scratch.Delete(recursive: true);
}

static async Task<int> Run(StreamClientTransport transport, CancellationToken cancellationToken,
    string source, byte[] original, string input, string output, string? signedSource, string backups)
{
    await using var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
    Require(tools.Count == 65 && tools.All(tool => !string.IsNullOrWhiteSpace(tool.Description)), "Tool descriptions missing.");
    var status = tools.Single(tool => tool.Name == McpToolNames.Status);
    Require(status.Description.StartsWith("Return the verified local UYAP Editor version", StringComparison.Ordinal),
        "The status descriptor did not use the expected resource.");

    var test = new AcceptanceClient(client, cancellationToken);
    var ready = await test.Call(McpToolNames.Status);
    Require(ready.GetProperty("bridge").GetString() == "ready for a managed document", "Unexpected initial bridge status.");
    await test.ExpectError(McpToolNames.Open, "PATH_OUTSIDE_SCOPE: Use an absolute .udf path",
        ("path", Path.Combine(Path.GetDirectoryName(input)!, "outside.udf")));
    var opened = await test.Call(McpToolNames.Open, ("path", source), ("editable", true));
    var id = opened.GetProperty("documentId").GetString()!;
    var page = await test.Call(McpToolNames.GoToPage, ("documentId", id), ("page", 3));
    Require(page.GetProperty("page").GetInt32() == 3, "Page navigation failed.");
    var pdf = Path.Combine(output, "synthetic.pdf");
    var exported = await test.Call(McpToolNames.ExportPdf, ("documentId", id), ("outputPath", pdf));
    Require(exported.GetProperty("pageCount").GetInt32() == 3 &&
        File.ReadAllBytes(pdf).AsSpan().StartsWith("%PDF-"u8), "The native PDF was invalid.");
    await PdfEditAcceptance.Run(test, id, output);
    await test.Call(McpToolNames.Close, ("documentId", id));

    await FileScopeAcceptance.Run(test, source, input, output, backups);
    await ArchiveAcceptance.Run(test, source, input, output);
    if (signedSource != null)
        await SignedImzAcceptance.Run(test, signedSource, output);
    await ApprovalAcceptance.Run(test, source, output);
    Require(original.AsSpan().SequenceEqual(File.ReadAllBytes(source)), "Source UDF changed.");
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        transport = "stdio",
        tools = tools.Count,
        page = 3,
        pdfPages = 3,
        pdfEditsAndUndoPassed = true,
        fileScopePassed = true,
        sourceUnchanged = true,
        resourceDescriptions = true,
        formattedResourceErrors = true,
        archivePassed = true,
        signedImzPassed = signedSource != null,
        approvals = 16,
    }));
    return 0;
}
