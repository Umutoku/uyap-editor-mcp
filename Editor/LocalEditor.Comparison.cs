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
            throw new EditorFailure(EditorErrors.DocumentModeRequired);
        if (decision is not ("inspect" or "accept_all" or "reject_all" or "accept_one" or "reject_one") ||
            (decision == "inspect") != (output == null))
            throw new EditorFailure(EditorErrors.ComparisonDecisionInvalid);
        var individual = decision is "accept_one" or "reject_one";
        if (individual ? string.IsNullOrWhiteSpace(changeId) || changeId.Length > 128 : changeId != null)
            throw new EditorFailure(EditorErrors.ComparisonChangeIdInvalid);
        if (limit is < 1 or > 50)
            throw new EditorFailure(EditorErrors.ComparisonLimitInvalid);
        if (offset < 0)
            throw new EditorFailure(EditorErrors.ComparisonOffsetInvalid);
        if (navigationDirection == null ? navigationFromChangeId != null
            : decision != "inspect" || navigationDirection is not ("next" or "previous") ||
              (navigationFromChangeId != null &&
               (string.IsNullOrWhiteSpace(navigationFromChangeId) || navigationFromChangeId.Length > 128)))
            throw new EditorFailure(EditorErrors.ComparisonNavigationInvalid);
        var baseline = settings.Input(baselinePath, allowOutputRoot: true);
        var target = output == null ? null : settings.Output(output, ".udf");
        if (target != null)
        {
            RequireEditable(session);
            PermitEditing(baseline);
        }
        var pending = target == null ? null : TemporaryOutput(target, ".udf");
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
            var headerLength = navigationDirection == null ? 1 : 6;
            if (values.Length < headerLength || (values.Length - headerLength) % 7 != 0)
                throw new EditorFailure(EditorErrors.ComparisonResponseInvalid);
            UdfChangeNavigation? navigation = navigationDirection == null ? null
                : values[1] == "true" ? new UdfChangeNavigation(navigationDirection + "-changes", true,
                    ReadNumber(values[2]), ReadNumber(values[3]), values[4], values[5])
                : new UdfChangeNavigation(navigationDirection + "-changes", false, null, null, null, null);
            var changes = values.Skip(headerLength).Chunk(7).Select(change =>
                new UdfChange(ReadNumber(change[0]), ReadNumber(change[1]), change[2], change[3], change[4],
                    ReadNumber(change[5]), ReadNumber(change[6]))).ToArray();
            var total = ReadNumber(values[0]);
            SavedCopy? saved = null;
            if (target != null && pending != null)
            {
                var valid = await ValidateOutput(pending, stream =>
                {
                    using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                    return zip.GetEntry("content.xml") != null
                        ? true
                        : throw new InvalidDataException("The UDF content member is missing.");
                }, token);
                token.ThrowIfCancellationRequested();
                EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
                Publish(pending, target);
                saved = new SavedCopy(target, valid.Bytes, valid.Digest);
            }
            var otherGroupsFrom = decision switch
            {
                "accept_one" => "baseline",
                "reject_one" => "current",
                _ => null,
            };
            int? nextOffset = offset + changes.Length < total ? offset + changes.Length : null;
            return new UdfComparison(decision, total, changes, saved, otherGroupsFrom, offset, nextOffset, navigation);
        }
        finally
        {
            if (comparisonEditor != null)
            {
                comparisonEditor.AbortStartup();
                comparisonEditor.Dispose();
            }
            if (pending != null)
                DeleteTemporaryOutput(pending, uncertain: false);
        }
    }, token);
}
