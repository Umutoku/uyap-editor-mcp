using System.ComponentModel;
using System.Globalization;

namespace UyapEditorMcp;

public sealed record ApprovalDocument(string DocumentId, string? OutputPath = null);

public sealed record ApprovalRequest(
    [property: Description("Exact UYAP action ID.")] string Action,
    [property: Description("Managed document IDs in review order. Supply explicit local output paths for any requested drafts or copies.")] ApprovalDocument[] Documents,
    string? DraftPath = null,
    string[]? Recipients = null,
    string? Subject = null,
    string? Message = null,
    string? Printer = null,
    int? Copies = null,
    string? PageRange = null,
    string? ProxyAddress = null,
    string? LibraryMode = null);

public sealed record ApprovalArtifact(string Path, string Format, long Bytes, string Sha256);
public sealed record ApprovalItem(string Label, string Value);

public sealed record ApprovalReview(string Action, string Status, ManualWorkflow? Workflow,
    ApprovalArtifact[] Artifacts, ApprovalItem[] Details, string[] Missing, string UserStep);

internal sealed partial class LocalEditor
{
    internal async Task<ApprovalReview> PrepareApproval(ApprovalRequest request, CancellationToken token)
    {
        if (request?.Documents is not { Length: >= 1 and <= 8 } documents ||
            documents.Any(document => document == null || string.IsNullOrWhiteSpace(document.DocumentId)) ||
            documents.Select(document => document.DocumentId).Distinct(StringComparer.Ordinal).Count() != documents.Length)
            throw new EditorFailure(EditorErrors.ApprovalDocumentsInvalid);

        if (documents.Length != 1 && request.Action is not (EditorActionIds.PrintBatch or EditorActionIds.SignBatch))
            throw new EditorFailure(EditorErrors.ApprovalDocumentCountInvalid);

        await Exclusive(() =>
        {
            foreach (var document in documents)
                _ = Get(document.DocumentId);
            return Task.FromResult(true);
        }, token);

        return await (request.Action switch
        {
            EditorActionIds.AttachUdf or EditorActionIds.AttachDoc or EditorActionIds.AttachOdt or EditorActionIds.AttachPdf =>
                PrepareMailApproval(request, token),
            EditorActionIds.PrintDocument or EditorActionIds.PrintBatch => PreparePrintApproval(request, token),
            EditorActionIds.ValidateCertificate => PrepareCertificateReview(request, token),
            EditorActionIds.ExportSignedDocument => PrepareSignedExportReview(request, token),
            EditorActionIds.SelectSigningLibrary => PrepareSigningLibraryReview(request, token),
            EditorActionIds.Sign => PrepareSigningReview(request, EditorWorkflowIds.Sign, token),
            EditorActionIds.SignMobile => PrepareSigningReview(request, EditorWorkflowIds.SignMobile, token),
            EditorActionIds.SignBatch => PrepareSigningReview(request, EditorWorkflowIds.SignBatch, token),
            EditorActionIds.SaveToPortal => PreparePortalApproval(request, token),
            EditorActionIds.SetProxy => PrepareProxyApproval(request, token),
            EditorActionIds.OpenHelp => PrepareHelpApproval(request, token),
            EditorActionIds.Update => PrepareUpdateApproval(request, token),
            _ => throw new EditorFailure(EditorErrors.ApprovalActionUnavailable),
        });
    }

    private static ApprovalArtifact[] PublishApprovalArtifacts(string[] stages, ApprovalArtifact[] artifacts,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var index = 0;
        try
        {
            for (; index < artifacts.Length; index++)
            {
                var target = artifacts[index].Path;
                EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
                Publish(stages[index], target);
            }
        }
        catch (Exception failure) when (index > 0 && failure is EditorFailure or IOException or UnauthorizedAccessException)
        {
            var published = string.Join(", ", artifacts.Take(index).Select(artifact => artifact.Path));
            throw new EditorFailure(EditorErrors.ApprovalOutputPartial, published, failure.Message);
        }
        return artifacts;
    }

    private async Task<ApprovalReview> PreparePortalApproval(ApprovalRequest request, CancellationToken token)
    {
        var document = request.Documents[0];
        var workflow = await Handoff(document.DocumentId, EditorWorkflowIds.SaveToPortal, token);
        ApprovalArtifact[] artifacts = [];
        if (!string.IsNullOrWhiteSpace(document.OutputPath))
        {
            var artifact = await ExportAttachmentUdf(document.DocumentId, document.OutputPath, token);
            artifacts = [new(artifact.Path, artifact.Format, artifact.Bytes, artifact.Sha256)];
        }

        return new ApprovalReview(request.Action, EditorWorkflowStatuses.BlockedPrerequisite, workflow, artifacts,
            [new("portalCommandAvailable", workflow.Enabled ? "true" : "false")],
            ["authorized_portal_session", "verified_portal_record"],
            ApprovalText.PortalApprovalStep);
    }

    private async Task<ApprovalReview> PrepareProxyApproval(ApprovalRequest request, CancellationToken token)
    {
        var workflow = await Handoff(request.Documents[0].DocumentId, EditorWorkflowIds.Proxy, token);
        var userStep = ApprovalText.ProxyApprovalStep;
        if (string.IsNullOrWhiteSpace(request.ProxyAddress))
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.InputRequired, workflow, [], [], ["proxyAddress"], userStep);

        if (!Uri.TryCreate(request.ProxyAddress, UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https" or "socks" or "socks5") ||
            address.HostNameType == UriHostNameType.Unknown || address.Port is < 1 or > 65535 ||
            address.UserInfo.Length != 0 || address.Query.Length != 0 || address.Fragment.Length != 0 ||
            address.AbsolutePath != "/")
            throw new EditorFailure(EditorErrors.ProxyAddressInvalid);

        return new ApprovalReview(request.Action, MenuApprovalStatus(workflow), workflow, [],
            [new("scheme", address.Scheme), new("host", address.IdnHost),
             new("port", address.Port.ToString(CultureInfo.InvariantCulture))],
            MenuMissing(workflow), userStep);
    }

    private async Task<ApprovalReview> PrepareHelpApproval(ApprovalRequest request, CancellationToken token)
    {
        var workflow = await Handoff(request.Documents[0].DocumentId, EditorWorkflowIds.Help, token);
        return new ApprovalReview(request.Action, MenuApprovalStatus(workflow), workflow, [],
            [new("editorHelpAddress", "http://www.uyap.gov.tr/editoryardim/")], MenuMissing(workflow),
            ApprovalText.HelpApprovalStep);
    }

    private async Task<ApprovalReview> PrepareUpdateApproval(ApprovalRequest request, CancellationToken token)
    {
        var workflow = await Handoff(request.Documents[0].DocumentId, EditorWorkflowIds.Update, token);
        return new ApprovalReview(request.Action, MenuApprovalStatus(workflow), workflow, [],
            [new("installedEditorVersion", EditorSettings.Version), new("updateCheck", EditorWorkflowStatuses.UserActionRequired)],
            MenuMissing(workflow),
            ApprovalText.UpdateApprovalStep);
    }

    private static string MenuApprovalStatus(ManualWorkflow workflow) =>
        workflow.Enabled ? EditorWorkflowStatuses.AwaitingUserConfirmation : EditorWorkflowStatuses.BlockedPrerequisite;

    private static string[] MenuMissing(ManualWorkflow workflow) =>
        workflow.Enabled ? [] : ["native_menu_available"];
}
