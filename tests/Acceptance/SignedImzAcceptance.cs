using System.Formats.Asn1;
using System.IO.Compression;
using static UyapEditorMcp.Acceptance.AcceptanceClient;

namespace UyapEditorMcp.Acceptance;

internal static class SignedImzAcceptance
{
    internal static async Task Run(AcceptanceClient client, string source, string output)
    {
        var original = File.ReadAllBytes(source);
        var opened = await client.Call(McpToolNames.Open, ("path", source));
        var id = opened.GetProperty("documentId").GetString();
        try
        {
            var imz = Path.Combine(output, "synthetic.imz");
            var result = await client.Call(McpToolNames.ExportSignedImz, ("documentId", id), ("outputPath", imz));
            var exported = File.ReadAllBytes(imz);
            Require(result.GetProperty("sourceUnchanged").GetBoolean() &&
                result.GetProperty("bytes").GetInt64() == exported.Length &&
                original.AsSpan().SequenceEqual(File.ReadAllBytes(source)) &&
                exported.AsSpan().SequenceEqual(ReadSignedData(source)), "Native IMZ export changed the signed data or source.");
        }
        finally
        {
            await client.Call(McpToolNames.Close, ("documentId", id));
        }
    }

    private static byte[] ReadSignedData(string source)
    {
        using var udf = ZipFile.OpenRead(source);
        using var signature = udf.GetEntry("sign.sgn")!.Open();
        using var bytes = new MemoryStream();
        signature.CopyTo(bytes);
        var contentInfo = new AsnReader(bytes.ToArray(), AsnEncodingRules.BER).ReadSequence();
        _ = contentInfo.ReadObjectIdentifier();
        return contentInfo.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true))
            .ReadEncodedValue().ToArray();
    }
}
