using System.Globalization;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    private async Task<ApprovalReview> PreparePrintApproval(ApprovalRequest request, CancellationToken token)
    {
        bool batch = request.Action == EditorActionIds.PrintBatch;
        if (batch ? request.Documents.Length < 2 : request.Documents.Length != 1)
            throw new EditorFailure(EditorErrors.PrintDocumentsInvalid);

        List<string> missing = [];
        if (request.Documents.Any(document => string.IsNullOrWhiteSpace(document.OutputPath)))
            missing.Add("documents.outputPath");
        if (string.IsNullOrWhiteSpace(request.Printer))
            missing.Add("printer");
        if (!request.Copies.HasValue)
            missing.Add("copies");
        if (string.IsNullOrWhiteSpace(request.PageRange))
            missing.Add("pageRange");
        var userStep = ApprovalText.PrintApprovalStep;
        if (missing.Count != 0)
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.InputRequired, null, [], [], missing.ToArray(), userStep);

        var printer = request.Printer!.Trim();
        if (printer.Length > 255 || printer.Any(char.IsControl))
            throw new EditorFailure(EditorErrors.PrintPrinterInvalid);
        var copies = request.Copies!.Value;
        if (copies < 1)
            throw new EditorFailure(EditorErrors.PrintCopiesInvalid);
        var (pages, lastPage) = ReadPrintPageRange(request.PageRange!);
        var targets = request.Documents.Select(document => settings.Output(document.OutputPath!, ".pdf")).ToArray();
        if (targets.Distinct(StringComparer.OrdinalIgnoreCase).Count() != targets.Length)
            throw new EditorFailure(EditorErrors.PrintOutputDuplicate);

        var workflow = await Handoff(request.Documents[0].DocumentId, batch ? EditorWorkflowIds.PrintBatch : EditorWorkflowIds.Print, token);
        ApprovalItem[] details =
        [
            new("Yazıcı", printer),
            new("Kopya", copies.ToString(CultureInfo.InvariantCulture)),
            new("Sayfalar", pages),
            new("Yazıcı doğrulaması", "Kullanıcının girdiği ad. Kurulu yazıcı ve çevrimiçi durum yazdırma penceresinde doğrulanır."),
        ];
        if (!workflow.Enabled)
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.BlockedPrerequisite, workflow, [], details, ["native_print_command"], userStep);

        // Stage the complete ordered package before exposing any of its destination PDFs.
        var pending = targets.Select(target => TemporaryOutput(target, ".pdf")).ToArray();
        List<PdfResult> previews = [];
        try
        {
            for (var index = 0; index < pending.Length; index++)
            {
                var pdf = await ExportPdf(request.Documents[index].DocumentId, pending[index], token);
                previews.Add(pdf);
                if (lastPage > pdf.PageCount)
                    throw new EditorFailure(EditorErrors.PrintPageRangeOutOfBounds, index + 1, pdf.PageCount);
            }

            var artifacts = PublishApprovalArtifacts(pending, previews.Select((pdf, index) =>
                new ApprovalArtifact(targets[index], "pdf", pdf.Bytes, pdf.Sha256)).ToArray(), token);
            ApprovalItem[] pageCounts =
                [.. previews.Select((pdf, index) => new ApprovalItem($"Belge {index + 1} sayfa sayısı",
                    pdf.PageCount.ToString(CultureInfo.InvariantCulture)))];
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.AwaitingUserConfirmation, workflow, artifacts,
                [.. details, .. pageCounts], [], userStep);
        }
        finally
        {
            // Only remove stages we know were completely written and validated.
            foreach (var preview in previews)
                DeleteTemporaryOutput(preview.Path, uncertain: false);
        }
    }

    private static (string Pages, int LastPage) ReadPrintPageRange(string value)
    {
        value = value.Trim();
        if (value.Equals("all", StringComparison.OrdinalIgnoreCase))
            return ("all", 0);
        if (value.Length > 512)
            throw new EditorFailure(EditorErrors.PrintPageRangeInvalid);

        List<string> ranges = [];
        int lastPage = 0;
        foreach (var part in value.Split(','))
        {
            var limits = part.Split('-', StringSplitOptions.TrimEntries);
            if (limits.Length is < 1 or > 2 ||
                !int.TryParse(limits[0], NumberStyles.None, CultureInfo.InvariantCulture, out int first) ||
                first <= lastPage ||
                !int.TryParse(limits[^1], NumberStyles.None, CultureInfo.InvariantCulture, out int last) ||
                last < first)
                throw new EditorFailure(EditorErrors.PrintPageRangeInvalid);
            ranges.Add(first == last
                ? first.ToString(CultureInfo.InvariantCulture)
                : FormattableString.Invariant($"{first}-{last}"));
            lastPage = last;
        }
        return (string.Join(',', ranges), lastPage);
    }
}
