namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    internal Task<TextResult> ReadText(string id, int start, int limit, CancellationToken token) => Exclusive(async () =>
    {
        var response = await Get(id).Editor.Call(EditorBridgeCommands.ReadText, token, WriteNumber(start), WriteNumber(limit));
        return new TextResult(start, ReadNumber(response[0]), response[1]);
    }, token);

    internal Task<SelectionResult> SelectText(string id, int start, int end, CancellationToken token) =>
        Exclusive(async () =>
        {
            var values = await Get(id).Editor.Call(EditorBridgeCommands.SelectText, token, WriteNumber(start), WriteNumber(end));
            return new SelectionResult(ReadNumber(values[0]), ReadNumber(values[1]));
        }, token);

    internal Task<FindResult> Find(string id, string query, int start, bool caseSensitive, CancellationToken token) =>
        Exclusive(async () =>
        {
            var values = await Get(id).Editor.Call(EditorBridgeCommands.Find, token, query, WriteNumber(start), caseSensitive ? "true" : "false");
            return new FindResult(values[0] != "-1", ReadNumber(values[0]), ReadNumber(values[1]));
        }, token);

    internal Task<SpellingResult> SpellCheck(string id, int start, int characterLimit, int issueLimit, CancellationToken token) =>
        Exclusive(async () =>
        {
            var values = await Get(id).Editor.Call(EditorBridgeCommands.SpellCheck, token,
                WriteNumber(start), WriteNumber(characterLimit), WriteNumber(issueLimit));
            var length = ReadNumber(values[0]);
            var next = ReadNumber(values[1]);
            var issues = new SpellingIssue[(values.Length - 2) / 4];
            for (var i = 0; i < issues.Length; i++)
            {
                var offset = 2 + i * 4;
                var suggestions = values[offset + 3];
                issues[i] = new SpellingIssue(ReadNumber(values[offset]), ReadNumber(values[offset + 1]),
                    values[offset + 2], suggestions.Length == 0 ? [] : suggestions.Split('\u001f'));
            }
            return new SpellingResult(length, next, next == length, issues);
        }, token);

    internal Task<EditResult> ApplySpelling(string id, int start, int end, string expectedWord, string suggestion,
        CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            var result = await session.Editor.Call(EditorBridgeCommands.SpellApply, token,
                WriteNumber(start), WriteNumber(end), expectedWord, suggestion);
            return new EditResult(ReadNumber(result[0]));
        }, token);

    internal Task<EditResult> Replace(string id, int start, int end, string text, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            if (text.Length > 8192)
                throw new EditorFailure(EditorErrors.TextLimit);
            var result = await session.Editor.Call(EditorBridgeCommands.Replace, token, WriteNumber(start), WriteNumber(end), text);
            return new EditResult(ReadNumber(result[0]));
        }, token);

    internal Task<CommentsResult> ListComments(string id, int offset, int limit, CancellationToken token) =>
        Exclusive(async () =>
        {
            var values = await Get(id).Editor.Call(EditorBridgeCommands.ListComments, token, WriteNumber(offset), WriteNumber(limit));
            if (values.Length == 0 || (values.Length - 1) % 5 != 0)
                throw new EditorFailure(EditorErrors.CommentListResponseInvalid);
            return new CommentsResult(ReadNumber(values[0]), offset,
                values.Skip(1).Chunk(5).Select(ReadComment).ToArray());
        }, token);

    internal Task<AutomaticTexts> ListAutomaticText(string id, CancellationToken token) =>
        Exclusive(async () => ParseAutomaticText(await Get(id).Editor.Call(EditorBridgeCommands.ListAutomaticText, token)), token);

    internal Task<AutomaticTexts> SetAutomaticText(string id, string key, string kind, string operation, string? text,
        string? richSourcePath, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        if (kind is not ("plain" or "rich" or "shortcut"))
            throw new EditorFailure(EditorErrors.AutoTextKindInvalid);
        if (operation is not ("set" or "delete"))
            throw new EditorFailure(EditorErrors.AutoTextOperationInvalid);
        var plain = kind == "plain" && operation == "set";
        var rich = kind is ("rich" or "shortcut") && operation == "set";
        if (plain != (text != null) || rich != (richSourcePath != null) || text?.Length > 8192)
            throw new EditorFailure(EditorErrors.AutoTextValueInvalid);
        string? staged = null;
        try
        {
            if (richSourcePath != null)
            {
                var source = documents.Input(richSourcePath, allowOutputRoot: true);
                var profile = Path.Combine(settings.StateDir, session.Id);
                staged = Path.Combine(profile, ".auto-text-" + Guid.NewGuid().ToString("N") + FileExtensions.Udf);
                File.Copy(source, staged, overwrite: false);
                PermitEditing(staged);
            }
            return ParseAutomaticText(await session.Editor.Call(EditorBridgeCommands.SetAutomaticText, token, key, kind, operation,
                staged ?? text ?? ""));
        }
        finally
        {
            if (staged != null)
                DocumentOutputRepository.DeleteTemporaryOutput(staged, session.Editor.IsUncertain);
        }
    }, token);

    internal Task<AutomaticTextInsertion> InsertAutomaticText(string id, string key, string kind,
        CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        var row = await session.Editor.Call(EditorBridgeCommands.InsertAutomaticText, token, key, kind);
        return new AutomaticTextInsertion(row[0], row[1], ReadNumber(row[2]), ReadNumber(row[3]));
    }, token);

    private static AutomaticTexts ParseAutomaticText(string[] response)
    {
        if (response.Length % 3 != 0)
            throw new EditorFailure(EditorErrors.AutomaticTextResponseInvalid);
        return new AutomaticTexts(response.Chunk(3).Select(row => new AutomaticTextEntry(row[0], row[1], row[2])).ToArray());
    }

    internal Task<EditorComment> AddComment(string id, int start, int end, string text, int type,
        CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        return ReadComment(await session.Editor.Call(EditorBridgeCommands.AddComment, token, WriteNumber(start), WriteNumber(end), text,
            WriteNumber(type)));
    }, token);

    internal Task<EditorComment> EditComment(string id, int index, int expectedStart, int expectedEnd,
        string expectedText, string text, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        return ReadComment(await session.Editor.Call(EditorBridgeCommands.EditComment, token, WriteNumber(index), WriteNumber(expectedStart),
            WriteNumber(expectedEnd), expectedText, text));
    }, token);

    internal Task<CommentChangeResult> ChangeComment(string id, int index, int expectedStart, int expectedEnd,
        int expectedType, string expectedText, string operation, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        var result = await session.Editor.Call(EditorBridgeCommands.ChangeComment, token, WriteNumber(index), WriteNumber(expectedStart),
            WriteNumber(expectedEnd), WriteNumber(expectedType), expectedText, operation);
        return new CommentChangeResult(ReadNumber(result[0]), index, ReadNumber(result[1]), operation == "delete");
    }, token);

    private static EditorComment ReadComment(string[] values)
    {
        if (values.Length != 5)
            throw new EditorFailure(EditorErrors.CommentResponseInvalid);
        return new EditorComment(ReadNumber(values[0]), ReadNumber(values[1]), ReadNumber(values[2]),
            ReadNumber(values[3]), values[4]);
    }

    internal Task<ImageResult> InsertImage(string id, string path, bool background, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            var image = documents.Image(path);
            var mode = background ? "insert-background-image" : "insert-image";
            var stagedImage = Path.Combine(Path.GetDirectoryName(session.Staged)!,
                "image-" + Guid.NewGuid().ToString("N") + Path.GetExtension(image));
            Directory.CreateDirectory(Path.GetDirectoryName(stagedImage)!);
            File.Copy(image, stagedImage, overwrite: false);
            var response = await session.Editor.Call(EditorBridgeCommands.Image, token, stagedImage, mode);
            return new ImageResult(Path.GetFileName(image), mode, response[0]);
        }, token);
}
