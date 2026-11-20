using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace UyapEditorMcp.Acceptance;

internal sealed class AcceptanceClient(McpClient client, CancellationToken cancellationToken)
{
    internal async Task<JsonElement> Call(string tool, params (string Name, object? Value)[] arguments)
    {
        var result = await Invoke(tool, arguments);
        Require(result.IsError is not true, $"{tool} returned an error. {JsonSerializer.Serialize(result.Content)}");
        return result.StructuredContent
            ?? throw new InvalidOperationException($"{tool} returned no structured output.");
    }

    internal async Task ExpectError(string tool, string message, params (string Name, object? Value)[] arguments)
    {
        var result = await Invoke(tool, arguments);
        Require(result.IsError is true &&
            result.Content.OfType<TextContentBlock>().Any(item => item.Text.Contains(message, StringComparison.Ordinal)),
            $"{tool} did not return {message}.");
    }

    private ValueTask<CallToolResult> Invoke(string tool, (string Name, object? Value)[] arguments) =>
        client.CallToolAsync(tool, arguments.ToDictionary(item => item.Name, item => item.Value),
            cancellationToken: cancellationToken);

    internal static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
