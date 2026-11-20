namespace UyapEditorMcp;

public sealed class ApprovalRequest
{
    [ResourceDescription(nameof(ApprovalContractText.Action))]
    public string Action { get; init; } = string.Empty;

    [ResourceDescription(nameof(ApprovalContractText.Documents))]
    public ApprovalDocument[] Documents { get; init; } = [];

    public string? DraftPath
    {
        get; init;
    }
    public string[]? Recipients
    {
        get; init;
    }
    public string? Subject
    {
        get; init;
    }
    public string? Message
    {
        get; init;
    }
    public string? Printer
    {
        get; init;
    }
    public int? Copies
    {
        get; init;
    }
    public string? PageRange
    {
        get; init;
    }
    public string? ProxyAddress
    {
        get; init;
    }
    public string? LibraryMode
    {
        get; init;
    }
}
