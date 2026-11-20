using System.Formats.Asn1;
using System.IO.Compression;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    internal Task<SignedImzExport> ExportSignedImz(string id, string output, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        if (session.TemplateMode || !Path.GetExtension(session.Staged).Equals(FileExtensions.Udf, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(session.Staged))
            throw new EditorFailure(EditorErrors.SignedUdfRequired);
        if (await session.Editor.HasSourceChanges(EditorErrors.SignedUdfChanged, token))
            throw new EditorFailure(EditorErrors.SignedUdfChanged);

        var target = documents.Output(output, FileExtensions.Imz);
        ReadOnlyMemory<byte> expected;
        using (var archive = ZipFile.OpenRead(session.Staged))
        {
            var entries = archive.Entries.Where(entry => entry.FullName == UdfEntries.Signature).ToArray();
            if (entries.Length != 1 || entries[0].Length is <= 0 or > EditorLimits.DocumentBytes)
                throw new EditorFailure(EditorErrors.SignatureEntryRequired);
            using var input = entries[0].Open();
            var signature = new byte[(int)entries[0].Length];
            await input.ReadExactlyAsync(signature, token);
            try
            {
                var container = new AsnReader(signature, AsnEncodingRules.BER);
                var content = container.ReadSequence();
                if (content.ReadObjectIdentifier() != "1.2.840.113549.1.7.2")
                    throw new AsnContentException();
                var wrapper = content.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));
                if (!wrapper.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
                    throw new AsnContentException();
                expected = wrapper.ReadEncodedValue();
                wrapper.ThrowIfNotEmpty();
                content.ThrowIfNotEmpty();
                container.ThrowIfNotEmpty();
            }
            catch (AsnContentException)
            {
                throw new EditorFailure(EditorErrors.UdfSignatureFormatInvalid);
            }
        }

        var pending = DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Imz);
        try
        {
            if (await session.Editor.Call(EditorBridgeCommands.SignedImz, token, pending) is not [EditorExportCommands.SignedImz])
                throw new EditorFailure(EditorErrors.SignedExportCommandMissing);
            var result = await DocumentOutputRepository.ValidateOutput(pending, stream =>
            {
                if (stream.Length != expected.Length)
                    throw new InvalidDataException("IMZ length differs from the archived SignedData.");
                var actual = new byte[expected.Length];
                stream.ReadExactly(actual);
                if (!actual.AsSpan().SequenceEqual(expected.Span))
                    throw new InvalidDataException("IMZ data differs from the archived SignedData.");
                return true;
            }, token);
            if (!OriginalUnchanged(session))
                throw new EditorFailure(EditorErrors.SourceChanged);
            token.ThrowIfCancellationRequested();
            DocumentOutputRepository.Publish(pending, target);
            return new SignedImzExport(target, result.Bytes, result.Digest, EditorExportCommands.SignedImz, true);
        }
        finally
        {
            DocumentOutputRepository.DeleteTemporaryOutput(pending, session.Editor.IsUncertain);
        }
    }, token);

    private async Task<ApprovalReview> PrepareCertificateReview(ApprovalRequest request, CancellationToken token)
    {
        var workflow = await Handoff(request.Documents[0].DocumentId, EditorWorkflowIds.Certificates, token);
        return new ApprovalReview(request.Action, EditorWorkflowStatuses.BlockedPrerequisite, workflow, [],
            [new("Kontrol", "Akıllı karttan seçilen sertifikanın geçerliliği")],
            ["smart_card_and_certificate"],
            ApprovalText.CertificateApprovalStep);
    }

    private async Task<ApprovalReview> PrepareSignedExportReview(ApprovalRequest request, CancellationToken token)
    {
        var document = request.Documents[0];
        string? target = document.OutputPath == null ? null : documents.Output(document.OutputPath, FileExtensions.Imz);
        var workflow = await Handoff(document.DocumentId, EditorWorkflowIds.ExportSigned, token);
        var signature = await Exclusive(async () =>
        {
            var session = Get(document.DocumentId);
            if (!File.Exists(session.Staged) || !string.Equals(Path.GetExtension(session.Staged), FileExtensions.Udf,
                    StringComparison.OrdinalIgnoreCase))
                return (Present: false, Name: Path.GetFileName(session.Staged), Sha256: (string?)null);
            var hasSourceChanges = await session.Editor.HasSourceChanges(
                EditorErrors.DocumentStateResponseInvalid, token);
            if (hasSourceChanges)
                return (Present: false, Name: Path.GetFileName(session.Staged), Sha256: (string?)null);
            LocalFilePaths.RequireExistingPlainPath(session.Staged);
            using var stream = new FileStream(session.Staged, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var entries = archive.Entries.Where(entry => entry.FullName == UdfEntries.Signature).ToArray();
            if (entries.Length != 1 || entries[0].Length == 0)
                return (Present: false, Name: Path.GetFileName(session.Staged), Sha256: (string?)null);
            stream.Position = 0;
            string digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
            return (Present: true, Name: Path.GetFileName(session.Staged), Sha256: (string?)digest);
        }, token);
        var details = new List<ApprovalItem> { new("Belge", signature.Name) };
        if (target != null)
            details.Add(new("IMZ hedefi", target));
        var step = ApprovalText.SignedExportApprovalStep;
        if (!signature.Present || !workflow.Enabled)
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.BlockedPrerequisite, workflow, [], details.ToArray(),
                [!signature.Present ? "signed_udf_required" : "native_command_unavailable"], step);
        details.Add(new("Belge SHA-256", signature.Sha256!));
        details.Add(new("İmza durumu", "UDF imza bölümü mevcut. Geçerlilik doğrulaması kullanıcı adımında yapılır."));
        return new ApprovalReview(request.Action, target == null ? EditorWorkflowStatuses.InputRequired : EditorWorkflowStatuses.AwaitingUserConfirmation,
            workflow, [], details.ToArray(), target == null ? ["output_path"] : [], step);
    }

    private async Task<ApprovalReview> PrepareSigningLibraryReview(ApprovalRequest request, CancellationToken token)
    {
        var mode = request.LibraryMode;
        if (mode is not null and not ("automatic" or "choose_provider"))
            throw new EditorFailure(EditorErrors.LibraryModeInvalid);
        var configured = await Exclusive(async () =>
        {
            var session = Get(request.Documents[0].DocumentId);
            // Open the existing cancel-only, local form and close this same revision under one gate.
            var form = ParseDialog(await session.Editor.Call(EditorBridgeCommands.OpenForm, token, EditorActionIds.SelectSigningLibrary));
            if (form.State != "open" || form.Revision == null)
                throw new EditorFailure(EditorErrors.SigningLibraryUnverified);
            try
            {
                var label = form.Controls.FirstOrDefault(control =>
                    control.Role == "message" && control.Value.Contains("Kütüphaneniz", StringComparison.Ordinal));
                if (label == null)
                    throw new EditorFailure(EditorErrors.SigningLibraryModeMissing);
                return label.Value.Contains("Otomatik", StringComparison.OrdinalIgnoreCase)
                    ? "Otomatik" : "Özel sağlayıcı";
            }
            finally
            {
                var closed = ParseDialog(await session.Editor.Call(EditorBridgeCommands.FormInput, CancellationToken.None,
                    form.Revision, "-1", "cancel", ""));
                if (closed.State != "closed")
                    throw new EditorFailure(EditorErrors.SigningLibraryCloseUnverified);
            }
        }, token);
        var details = new List<ApprovalItem> { new("Mevcut seçim", configured) };
        if (mode != null)
            details.Add(new("Önerilen seçim", mode == "automatic" ? "Otomatik" : "Sağlayıcıyı yerel pencerede seç"));
        var step = ApprovalText.SigningLibraryApprovalStep;
        var status = mode == null ? EditorWorkflowStatuses.InputRequired : EditorWorkflowStatuses.AwaitingUserConfirmation;
        var workflow = new ManualWorkflow(EditorWorkflowIds.SigningLibrary, EditorMenuLabels.Tools, "İmza Kütüphanesi", true, EditorWorkflowStatuses.UserActionRequired);
        return new ApprovalReview(request.Action, status, workflow, [], details.ToArray(),
            mode == null ? ["library_mode"] : [], step);
    }

    private async Task<ApprovalReview> PrepareSigningReview(ApprovalRequest request, string mode, CancellationToken token)
    {
        var workflow = await Handoff(request.Documents[0].DocumentId, mode, token);
        string step = mode switch
        {
            EditorWorkflowIds.SignMobile => ApprovalText.MobileSigningApprovalStep,
            EditorWorkflowIds.SignBatch => ApprovalText.BatchSigningApprovalStep,
            _ => ApprovalText.SigningApprovalStep,
        };
        ApprovalItem[] details = [new("Belge sayısı", request.Documents.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))];
        if (!workflow.Enabled)
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.BlockedPrerequisite, workflow, [], details,
                ["native_command_unavailable"], step);
        if (request.Documents.Any(document => string.IsNullOrWhiteSpace(document.OutputPath)))
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.InputRequired, workflow, [], details, ["output_path"], step);

        var targets = request.Documents.Select(document => documents.Output(document.OutputPath!, FileExtensions.Udf)).ToArray();
        if (targets.Distinct(StringComparer.OrdinalIgnoreCase).Count() != targets.Length)
            throw new EditorFailure(EditorErrors.SigningSelectionInvalid);
        var stages = targets.Select(target => DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Udf)).ToArray();
        List<FormatExport> snapshots = [];
        try
        {
            for (var index = 0; index < stages.Length; index++)
                snapshots.Add(await ExportAttachmentUdf(request.Documents[index].DocumentId, stages[index], token));
            var artifacts = PublishApprovalArtifacts(stages, snapshots.Select((snapshot, index) =>
                new ApprovalArtifact(targets[index], snapshot.Format, snapshot.Bytes, snapshot.Sha256)).ToArray(), token);
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.AwaitingUserConfirmation, workflow, artifacts, details, [], step);
        }
        finally
        {
            foreach (var snapshot in snapshots)
                DocumentOutputRepository.DeleteTemporaryOutput(snapshot.Path, uncertain: false);
        }
    }
}
