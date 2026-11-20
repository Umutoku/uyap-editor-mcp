using System.Security.Cryptography;
using System.Text.Json;
using static UyapEditorMcp.Acceptance.AcceptanceClient;

namespace UyapEditorMcp.Acceptance;

internal static class ApprovalAcceptance
{
    internal static async Task Run(AcceptanceClient client, string source, string output)
    {
        var first = await Open(client, source);
        var second = await Open(client, source);
        try
        {
            await CheckMail(client, first, output);
            await CheckPrint(client, first, second, output);
            await CheckSecurity(client, first, second, output);

            var document = new
            {
                documentId = first
            };
            await Check(client, EditorActionIds.SaveToPortal, EditorWorkflowStatuses.BlockedPrerequisite,
                new
                {
                    documents = new[] { document }
                });
            await Check(client, EditorActionIds.SetProxy, EditorWorkflowStatuses.AwaitingUserConfirmation,
                new
                {
                    documents = new[] { document },
                    proxyAddress = "http://proxy.example.invalid:8080"
                });
            await Check(client, EditorActionIds.OpenHelp, EditorWorkflowStatuses.AwaitingUserConfirmation,
                new
                {
                    documents = new[] { document }
                });
            await Check(client, EditorActionIds.Update, EditorWorkflowStatuses.AwaitingUserConfirmation,
                new
                {
                    documents = new[] { document }
                });
        }
        finally
        {
            await client.Call(McpToolNames.Close, ("documentId", second));
            await client.Call(McpToolNames.Close, ("documentId", first));
        }
    }

    private static async Task CheckMail(AcceptanceClient client, string id, string output)
    {
        (string Action, string Format)[] actions =
        [
            (EditorActionIds.AttachUdf, "udf"), (EditorActionIds.AttachDoc, "doc"),
            (EditorActionIds.AttachOdt, "odt"), (EditorActionIds.AttachPdf, "pdf"),
        ];
        foreach (var (action, format) in actions)
        {
            var attachment = Path.Combine(output, $"attachment.{format}");
            var draft = Path.Combine(output, $"attachment-{format}.eml");
            var result = await Check(client, action, EditorWorkflowStatuses.AwaitingUserConfirmation, new
            {
                documents = new[] { new { documentId = id, outputPath = attachment } },
                draftPath = draft,
                recipients = new[] { "test@example.invalid" },
                subject = "Kurgu ek",
                message = "Kurgu belge eki için yerel kabul.",
            });
            Require(result.GetProperty("artifacts").GetArrayLength() == 2 &&
                File.ReadAllText(draft).Contains("X-Unsent: 1", StringComparison.Ordinal), "Mail draft was not prepared.");
        }
    }

    private static async Task CheckPrint(AcceptanceClient client, string first, string second, string output)
    {
        var printer = "Microsoft Print to PDF";
        var documents = new[]
        {
            new { documentId = first, outputPath = Path.Combine(output, "single-print.pdf") },
        };
        await Check(client, EditorActionIds.PrintDocument, EditorWorkflowStatuses.AwaitingUserConfirmation,
            new
            {
                documents,
                printer,
                copies = 1,
                pageRange = "all"
            });
        documents =
        [
            new { documentId = first, outputPath = Path.Combine(output, "batch-print-first.pdf") },
            new { documentId = second, outputPath = Path.Combine(output, "batch-print-second.pdf") },
        ];
        await Check(client, EditorActionIds.PrintBatch, EditorWorkflowStatuses.AwaitingUserConfirmation,
            new
            {
                documents,
                printer,
                copies = 1,
                pageRange = "all"
            });
    }

    private static async Task CheckSecurity(AcceptanceClient client, string first, string second, string output)
    {
        var documents = new[] { new { documentId = first } };
        await Check(client, EditorActionIds.ValidateCertificate, EditorWorkflowStatuses.BlockedPrerequisite,
            new
            {
                documents
            });
        await Check(client, EditorActionIds.ExportSignedDocument, EditorWorkflowStatuses.BlockedPrerequisite,
            new
            {
                documents
            });
        await Check(client, EditorActionIds.SelectSigningLibrary, EditorWorkflowStatuses.AwaitingUserConfirmation,
            new
            {
                documents,
                libraryMode = "automatic"
            });
        foreach (var action in new[] { EditorActionIds.Sign, EditorActionIds.SignMobile })
        {
            await Check(client, action, EditorWorkflowStatuses.AwaitingUserConfirmation, new
            {
                documents = new[] { new { documentId = first, outputPath = Path.Combine(output, $"{action}.udf") } },
            });
        }
        await Check(client, EditorActionIds.SignBatch, EditorWorkflowStatuses.AwaitingUserConfirmation, new
        {
            documents = new[]
            {
                new { documentId = first, outputPath = Path.Combine(output, "sign-batch-first.udf") },
                new { documentId = second, outputPath = Path.Combine(output, "sign-batch-second.udf") },
            },
        });
    }

    private static async Task<string> Open(AcceptanceClient client, string source) =>
        (await client.Call(McpToolNames.Open, ("path", source))).GetProperty("documentId").GetString()!;

    private static async Task<JsonElement> Check(AcceptanceClient client, string action, string status, object parameters)
    {
        var request = JsonSerializer.SerializeToElement(parameters).EnumerateObject()
            .ToDictionary(property => property.Name, property => (object)property.Value);
        request.Add("action", action);
        var result = await client.Call(McpToolNames.PrepareApproval, ("request", request));
        Require(result.GetProperty("action").GetString() == action &&
            result.GetProperty("status").GetString() == status, $"Unexpected approval response for {action}.");
        foreach (var artifact in result.GetProperty("artifacts").EnumerateArray())
        {
            var data = File.ReadAllBytes(artifact.GetProperty("path").GetString()!);
            Require(data.Length == artifact.GetProperty("bytes").GetInt64() &&
                Convert.ToHexString(SHA256.HashData(data)) == artifact.GetProperty("sha256").GetString(),
                $"Invalid artifact for {action}.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            action,
            status
        }));
        return result;
    }
}
