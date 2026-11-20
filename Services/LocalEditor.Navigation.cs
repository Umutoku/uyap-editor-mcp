using System.Globalization;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    internal Task<PageResult> Page(string id, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        // The Swing response confirms a responsive managed process before querying the external bridge.
        var state = await session.Editor.Call(EditorBridgeCommands.Page, token);
        return await bridge.Page(session.Editor.Pid, token) ?? ParsePage(state);
    }, token);

    internal Task<PageResult> Go(string id, int page, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        var returned = ParsePage(await session.Editor.Call(EditorBridgeCommands.Go, token, WriteNumber(page)));
        var seen = await bridge.Page(session.Editor.Pid, token);
        if (seen != null && (seen.Page != returned.Page || seen.PageCount != returned.PageCount))
            throw new EditorException(EditorErrors.PagePositionUnverified);
        return seen ?? returned;
    }, token);

    internal Task<PageRegionsResult> ReadPageRegions(string id, CancellationToken token) => Exclusive(async () =>
    {
        var values = await Get(id).Editor.Call(EditorBridgeCommands.PageRegions, token);
        return new PageRegionsResult(values.Chunk(5).Select(ReadPageRegion).ToArray());
    }, token);

    internal Task<PageRegion> SetPageRegion(string id, string region, string text, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            return ReadPageRegion(await session.Editor.Call(EditorBridgeCommands.SetPageRegion, token, region, text));
        }, token);

    internal Task<PageRegionRange> PageRegionRange(string id, string region, int? firstPage, int? lastPage,
        CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        if (firstPage.HasValue || lastPage.HasValue)
            RequireEditable(session);
        if (region is not ("header" or "footer") || firstPage is < 1 || (lastPage is < 1 && lastPage != -1))
            throw new EditorException(EditorErrors.PageRegionRangeInvalid);
        var values = await session.Editor.Call(EditorBridgeCommands.PageRegionRange, token, region,
            firstPage.HasValue ? WriteNumber(firstPage.Value) : "", lastPage.HasValue ? WriteNumber(lastPage.Value) : "");
        if (values.Length != 3)
            throw new EditorException(EditorErrors.PageRegionRangeResponseInvalid);
        return new PageRegionRange(values[0], ReadNumber(values[1]), values[2].Length == 0 ? null : ReadNumber(values[2]));
    }, token);

    private static PageRegion ReadPageRegion(string[] values)
    {
        if (values.Length != 5)
            throw new EditorException(EditorErrors.PageRegionResponseInvalid);
        return new PageRegion(values[0], values[1] == "true", ReadNumber(values[2]), ReadNumber(values[3]), values[4]);
    }

    internal Task<EditorViewResult> ViewState(string id, CancellationToken token) => Exclusive(async () =>
        ParseView(await Get(id).Editor.Call(EditorBridgeCommands.ViewState, token)), token);

    internal Task<EditorViewResult> SetView(string id, int? zoomPercent, bool? showWhitespace, CancellationToken token) =>
        Exclusive(async () =>
        {
            if (zoomPercent == null && showWhitespace == null || zoomPercent is < 10 or > 825)
                throw new EditorException(EditorErrors.ViewOptionsInvalid);
            var values = await Get(id).Editor.Call(EditorBridgeCommands.SetView, token,
                zoomPercent.HasValue ? WriteNumber(zoomPercent.Value) : "",
                showWhitespace.HasValue ? (showWhitespace.Value ? "true" : "false") : "");
            return ParseView(values);
        }, token);

    private static EditorViewResult ParseView(string[] values) => new(ReadNumber(values[0]), bool.Parse(values[1]),
        ReadNumber(values[2]), ReadNumber(values[3]), ReadNumber(values[4]), ReadNumber(values[5]),
        double.Parse(values[6], CultureInfo.InvariantCulture), values.Skip(8).Select(ReadNumber).ToArray(),
        bool.Parse(values[7]));

    internal Task<EditorShortcutsResult> Shortcuts(string id, int offset, int limit, CancellationToken token) =>
        Exclusive(async () =>
        {
            if (offset < 0 || limit is < 1 or > 100)
                throw new EditorException(EditorErrors.RangeInvalid);
            var values = await Get(id).Editor.Call(EditorBridgeCommands.Shortcuts, token, WriteNumber(offset), WriteNumber(limit));
            return new EditorShortcutsResult(ReadNumber(values[0]), offset, values.Skip(1).Chunk(3)
                .Select(fields => new EditorShortcut(fields[0], fields[1], fields[2])).ToArray());
        }, token);
}
