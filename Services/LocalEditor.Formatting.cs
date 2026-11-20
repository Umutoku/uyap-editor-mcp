using System.Globalization;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    internal Task<EditorFontResult> SetFont(string id, string? family, int? size, CancellationToken token) =>
        Exclusive(async () =>
        {
            if (family == "" || family is { Length: > 128 } || size is < 1 or > 255 || (family == null && size == null))
                throw new EditorFailure(EditorErrors.FontOptionsInvalid);
            var session = Get(id);
            RequireEditable(session);
            var values = await session.Editor.Call(EditorBridgeCommands.SetFont, token, family ?? "", size.HasValue ? WriteNumber(size.Value) : "");
            return new EditorFontResult(ReadNumber(values[0]), ReadNumber(values[1]), family, size);
        }, token);

    internal Task<PaletteColorResult> SetPaletteColor(string id, string target, string color, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            var values = await session.Editor.Call(EditorBridgeCommands.SetPaletteColor, token, target, color);
            var start = ReadNumber(values[1]);
            var end = ReadNumber(values[2]);
            return new PaletteColorResult(target, values[0], start < 0 ? "editor_profile" : "document",
                start < 0 ? null : start, end < 0 ? null : end);
        }, token);

    internal Task<LineSpacingResult> SetLineSpacing(string id, double multiplier, CancellationToken token) =>
        Exclusive(async () =>
        {
            if (multiplier is not (1 or 1.15 or 2 or 2.5 or 3))
                throw new EditorFailure(EditorErrors.LineSpacingUnavailable);
            var session = Get(id);
            RequireEditable(session);
            var values = await session.Editor.Call(EditorBridgeCommands.SetLineSpacing, token,
                multiplier.ToString("0.0#", CultureInfo.InvariantCulture));
            return new LineSpacingResult(multiplier, ReadNumber(values[0]), ReadNumber(values[1]));
        }, token);

    internal Task<FormatBrushResult> ApplyFormatBrush(string id, int sourceOffset, int start, int end,
        bool includeParagraph, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            var values = await session.Editor.Call(EditorBridgeCommands.FormatBrush, token, WriteNumber(sourceOffset), WriteNumber(start),
                WriteNumber(end), includeParagraph ? "true" : "false");
            return new FormatBrushResult(sourceOffset, ReadNumber(values[0]), ReadNumber(values[1]), includeParagraph);
        }, token);

    internal Task<ListPaletteResult> SetListPalette(string id, string preset, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            var values = await session.Editor.Call(EditorBridgeCommands.SetListPalette, token, preset);
            return new ListPaletteResult(preset, values[0].Length == 0 ? null : values[0], ReadNumber(values[1]),
                ReadNumber(values[2]));
        }, token);

    internal Task<ListItemResult> GetListItem(string id, CancellationToken token) =>
        Exclusive(async () => ReadListItem(await Get(id).Editor.Call(EditorBridgeCommands.GetListItem, token)), token);

    internal Task<CustomListResult> SetCustomList(string id, ListLevelFormat[] levels, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);

            if (levels is null || levels.Length is < 1 or > EditorLimits.ListLevels)
                throw new EditorFailure(EditorErrors.ListLevelFormatCountInvalid);
            var formats = new string[EditorLimits.ListLevels];
            Array.Fill(formats, string.Empty);
            foreach (var level in levels)
            {
                if (level is null || level.Level is < 1 or > EditorLimits.ListLevels || formats[level.Level - 1].Length != 0)
                    throw new EditorFailure(EditorErrors.ListLevelFormatsInvalid);
                if (string.IsNullOrWhiteSpace(level.Format) || level.Format.Length > 48)
                    throw new EditorFailure(EditorErrors.ListNumberFormatInvalid);
                formats[level.Level - 1] = level.Format;
            }

            var result = await session.Editor.Call(EditorBridgeCommands.SetCustomList, token, formats);
            return new CustomListResult(ReadNumber(result[0]), bool.Parse(result[1]));
        }, token);

    internal Task<ListItemResult> SetListLevel(string id, int level, CancellationToken token) =>
        ChangeListItem(id, "set_list_level", token, WriteNumber(level));

    internal Task<ListIndentsResult> SetListIndents(string id, ListLevelIndent[] levels, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);

            if (levels is null || levels.Length is < 1 or > EditorLimits.ListLevels)
                throw new EditorFailure(EditorErrors.ListLevelIndentCountInvalid);
            var indents = new string[EditorLimits.ListLevels];
            Array.Fill(indents, string.Empty);
            foreach (var level in levels)
            {
                if (level is null || level.Level is < 1 or > EditorLimits.ListLevels || indents[level.Level - 1].Length != 0)
                    throw new EditorFailure(EditorErrors.ListLevelIndentsInvalid);
                if (!float.IsFinite(level.IndentPoints) || level.IndentPoints is < 0 or > short.MaxValue)
                    throw new EditorFailure(EditorErrors.ListIndentInvalid);
                indents[level.Level - 1] = level.IndentPoints.ToString("R", CultureInfo.InvariantCulture);
            }
            var result = await session.Editor.Call(EditorBridgeCommands.SetListIndents, token, indents);
            return new ListIndentsResult(ReadNumber(result[0]));
        }, token);

    internal Task<ListItemResult> SetNumberingStart(string id, int startAt, CancellationToken token) =>
        ChangeListItem(id, "set_numbering_start", token, WriteNumber(startAt));

    internal Task<ListItemResult> ContinueNumbering(string id, CancellationToken token) =>
        ChangeListItem(id, "continue_numbering", token);

    private Task<ListItemResult> ChangeListItem(string id, string command, CancellationToken token,
        params string[] arguments) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            return ReadListItem(await session.Editor.Call(command, token, arguments));
        }, token);

    private static ListItemResult ReadListItem(string[] values)
    {

        var formats = values.Skip(5).Take(EditorLimits.ListLevels).Select((format, index) => new ListLevelFormat(index + 1, format))
            .Where(level => level.Format.Length != 0).ToArray();
        return new ListItemResult(ReadNumber(values[0]), ReadNumber(values[1]),
            values[2].Length == 0 ? null : ReadNumber(values[2]), bool.Parse(values[3]),
            values[4].Length == 0 ? null : ReadNumber(values[4]), formats.Length == 0 ? null : formats,
            values.Length > 5 + EditorLimits.ListLevels ? float.Parse(values[5 + EditorLimits.ListLevels], CultureInfo.InvariantCulture) : null);
    }
}
