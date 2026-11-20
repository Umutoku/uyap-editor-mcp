using System.IO.Compression;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    internal Task<UdfComparison> CompareUdf(string id, string baselinePath, string decision, string? output,
        int limit, string? changeId, int offset, string? navigationDirection, string? navigationFromChangeId,
        CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        if (session.TemplateMode)
            throw new EditorException(EditorErrors.DocumentModeRequired);
        ValidateComparison(decision, output, limit, changeId, offset, navigationDirection, navigationFromChangeId);
        var baseline = documents.Input(baselinePath, allowOutputRoot: true);
        var target = output == null ? null : documents.Output(output, FileExtensions.Udf);
        if (target != null)
        {
            RequireEditable(session);
            PermitEditing(baseline);
        }
        var pending = target == null ? null : DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Udf);
        var baselineDirectory = Path.Combine(Path.GetDirectoryName(session.Staged)!, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baselineDirectory);
        var stagedBaseline = Path.Combine(baselineDirectory, Path.GetFileName(baseline));
        File.Copy(baseline, stagedBaseline, overwrite: false);
        var comparisonProfile = Path.Combine(baselineDirectory, "editor");
        Directory.CreateDirectory(comparisonProfile);
        var snapshot = Path.Combine(comparisonProfile, "kurgu-karsilastirma.udf");
        EditorProcess? comparisonEditor = null;
        try
        {
            await session.Editor.Call(EditorBridgeCommands.SnapshotUdf, token, snapshot);
            comparisonEditor = new EditorProcess(settings, comparisonProfile, snapshot, createBlank: false);
            await comparisonEditor.Call(EditorBridgeCommands.Ready, token);
            var values = await comparisonEditor.Call(EditorBridgeCommands.CompareUdf, token, stagedBaseline, decision, pending ?? "",
                WriteNumber(limit), changeId ?? "", WriteNumber(offset), navigationDirection ?? "",
                navigationFromChangeId ?? "");
            var result = ReadComparison(values, decision, offset, navigationDirection);
            if (target != null && pending != null)
                return result with { Output = await PublishComparison(pending, target, token) };
            return result;
        }
        finally
        {
            if (comparisonEditor != null)
            {
                comparisonEditor.AbortStartup();
                comparisonEditor.Dispose();
            }
            if (pending != null)
                DocumentOutputRepository.DeleteTemporaryOutput(pending, uncertain: false);
        }
    }, token);

    private static void ValidateComparison(string decision, string? output, int limit, string? changeId, int offset,
        string? navigationDirection, string? navigationFromChangeId)
    {
        if (decision is not ("inspect" or "accept_all" or "reject_all" or "accept_one" or "reject_one") ||
            (decision == "inspect") != (output == null))
            throw new EditorException(EditorErrors.ComparisonDecisionInvalid);
        var individual = decision is "accept_one" or "reject_one";
        if (individual ? !IsComparisonChangeIdValid(changeId) : changeId != null)
            throw new EditorException(EditorErrors.ComparisonChangeIdInvalid);
        if (limit is < 1 or > 50)
            throw new EditorException(EditorErrors.ComparisonLimitInvalid);
        if (offset < 0)
            throw new EditorException(EditorErrors.ComparisonOffsetInvalid);
        if (navigationDirection == null ? navigationFromChangeId != null
            : decision != "inspect" || navigationDirection is not ("next" or "previous") ||
              navigationFromChangeId != null && !IsComparisonChangeIdValid(navigationFromChangeId))
            throw new EditorException(EditorErrors.ComparisonNavigationInvalid);
    }

    private static bool IsComparisonChangeIdValid(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= 128;

    private static UdfComparison ReadComparison(string[] values, string decision, int offset, string? direction)
    {
        var headerLength = direction == null ? 1 : 6;
        if (values.Length < headerLength || (values.Length - headerLength) % 7 != 0)
            throw new EditorException(EditorErrors.ComparisonResponseInvalid);

        var navigation = direction == null ? null : ReadChangeNavigation(direction, values);
        var changes = values.Skip(headerLength).Chunk(7).Select(change =>
            new UdfChange(ReadNumber(change[0]), ReadNumber(change[1]), change[2], change[3], change[4],
                ReadNumber(change[5]), ReadNumber(change[6]))).ToArray();
        var total = ReadNumber(values[0]);
        var otherGroupsFrom = decision switch
        {
            "accept_one" => "baseline",
            "reject_one" => "current",
            _ => null,
        };
        int? nextOffset = offset + changes.Length < total ? offset + changes.Length : null;
        return new UdfComparison(decision, total, changes, null, otherGroupsFrom, offset, nextOffset, navigation);
    }

    private static UdfChangeNavigation ReadChangeNavigation(string direction, string[] values) =>
        values[1] == "true"
            ? new UdfChangeNavigation(direction + "-changes", true, ReadNumber(values[2]), ReadNumber(values[3]),
                values[4], values[5])
            : new UdfChangeNavigation(direction + "-changes", false, null, null, null, null);

    private static async Task<SavedCopy> PublishComparison(string pending, string target, CancellationToken token)
    {
        var valid = await DocumentOutputRepository.ValidateOutput(pending, stream =>
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return zip.GetEntry(UdfEntries.Content) != null
                ? true
                : throw new InvalidDataException("The UDF content member is missing.");
        }, token);
        token.ThrowIfCancellationRequested();
        DocumentOutputRepository.Publish(pending, target);
        return new SavedCopy(target, valid.Bytes, valid.Digest);
    }
}
