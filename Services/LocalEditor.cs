using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor(EditorSettings settings, LocalDocumentRepository documents, BridgeReader bridge) : IDisposable
{
    private sealed record Session(string Id, string? Original, string Staged, string? Digest, bool Editable, EditorProcess Editor,
        bool TemplateMode, bool EditingAllowed);

    private readonly Dictionary<string, Session> sessions = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim gate = new(1, 1);
    private static readonly string[] Capabilities =
    [
        "Native UDF open, blank creation and page navigation", "Native Şablon Editörü fields, groups and forms",
        "Live open/save filters and native document import",
        "Native PDF, UDF, selectable format and batch export", "Embedded pictures and page backgrounds",
        "Text reading, selection, search, editing and an isolated document clipboard",
        "Nested native menu inventory, Swing actions and managed windows",
        "Native anchored comments and Turkish spelling suggestions",
        "Native UDF version comparison and resolution",
        "Native backup recovery and managed-profile automatic text",
        "Native page regions, zoom, fonts and keyboard shortcut discovery",
        "Local formatting, native macro recording and replay, lists, tables and scoped dialog commands",
        "User-controlled handoff for signing, physical printing, sending and UYAP portal submission"
    ];

    internal Task<StatusResult> Status(CancellationToken token) => Exclusive(async () =>
    {
        var current = sessions.Values.LastOrDefault(session => !session.Editor.Exited && !session.Editor.IsUncertain);
        var bridgeState = BridgeState.BridgeReady;
        if (current != null)
        {
            var page = await bridge.Page(current.Editor.Pid, token);
            bridgeState = page == null ? bridge.Status : BridgeState.BridgeConnected;
        }

        return new StatusResult(EditorSettings.Version, "Windows x64",
            sessions.Values.Count(session => !session.Editor.Exited),
            EditorResources.Get(bridgeState.ToString()),
            Capabilities);
    }, token);

    internal Task<OpenResult> Open(string path, bool editable, CancellationToken token,
        string? capitalizationFromDocumentId = null) => Exclusive(async () =>
    {
        var source = documents.Input(path, allowOutputRoot: true);
        var capitalization = capitalizationFromDocumentId == null ? null :
            await ReadCapitalization(Get(capitalizationFromDocumentId), token);
        return await Start(source, editable, token, capitalization: capitalization);
    }, token);

    internal Task<CapitalizationState> ReadCapitalization(string id, CancellationToken token) =>
        Exclusive(() => ReadCapitalization(Get(id), token), token);

    private static async Task<CapitalizationState> ReadCapitalization(Session session, CancellationToken token)
    {
        var values = await session.Editor.Call(EditorBridgeCommands.CapitalizationState, token);
        if (values.Length != 4 || values.Any(value => value is not (EditorWireValues.True or EditorWireValues.False)))
            throw new EditorException(EditorErrors.CapitalizationResponseInvalid);
        return new CapitalizationState(values[0] == EditorWireValues.True, values[1] == EditorWireValues.True, values[2] == EditorWireValues.True, values[3] == EditorWireValues.True);
    }

    internal Task<OpenResult> ImportDocument(string path, CancellationToken token) => Exclusive(async () =>
    {
        var source = documents.ImportDocument(path);
        return await Start(source, editable: false, token);
    }, token);

    internal Task<EditorFormats> ListFormats(string id, CancellationToken token) => Exclusive(async () =>
    {
        var values = await Get(id).Editor.Call(EditorBridgeCommands.Formats, token);
        var filters = values.Chunk(2).ToArray();
        if (filters.Any(pair => pair.Length != 2 || pair[0] is not ("open" or "save")))
            throw new EditorException(EditorErrors.FileFilterResponseInvalid);
        return new EditorFormats(filters.Where(pair => pair[0] == "open").Select(pair => pair[1]).ToArray(),
            filters.Where(pair => pair[0] == "save").Select(pair => pair[1]).ToArray());
    }, token);

    internal Task<OpenResult> New(CancellationToken token) => Exclusive(() => Start(null, true, token), token);

    internal Task<OpenResult> OpenTemplate(string? path, CancellationToken token) => Exclusive(() =>
        Start(path == null ? null : documents.Input(path, allowOutputRoot: true), true, token, templateMode: true), token);

    internal Task<EditResult> ReuseTemplateField(string id, string fieldName, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            RequireEditable(session);
            var result = await session.Editor.Call(EditorBridgeCommands.TemplateReuse, token, fieldName);
            return new EditResult(ReadNumber(result[0]));
        }, token);

    private async Task<OpenResult> Start(string? source, bool editable, CancellationToken token, bool templateMode = false,
        CapitalizationState? capitalization = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var profile = Path.Combine(settings.StateDir, id);
        CheckSessions();
        Directory.CreateDirectory(profile);
        var copy = source == null ? Path.Combine(templateMode ? profile : Path.Combine(profile, EditorProfilePaths.SettingsFolder), "isimsiz.UDF") :
            Path.Combine(profile, Path.GetFileName(source));
        string? digest = null;
        if (source != null)
        {
            File.Copy(source, copy, overwrite: false);
            if (Path.GetExtension(copy).Equals(FileExtensions.Xml, StringComparison.OrdinalIgnoreCase))
                ValidateImportXml(copy);
            if (!templateMode && Path.GetExtension(copy).Equals(FileExtensions.Usf, StringComparison.OrdinalIgnoreCase))
                ValidateImportUsf(copy);
            if (editable)
                PermitEditing(copy);
            digest = LocalFilePaths.Sha256(copy);
        }
        var rtf = Path.GetExtension(copy).Equals(FileExtensions.Rtf, StringComparison.OrdinalIgnoreCase);
        if (rtf)
            ValidateImportRtf(copy);
        if (capitalization != null)
            WriteCapitalizationProfile(profile, capitalization);
        var launchPath = rtf ? Path.Combine(profile, EditorProfilePaths.SettingsFolder, "isimsiz.UDF") : copy;
        var editor = new EditorProcess(settings, profile, launchPath, createBlank: source == null || rtf, templateMode);
        try
        {
            var opening = await editor.Call(EditorBridgeCommands.Ready, token);
            if (rtf)
                opening = await editor.Call(EditorBridgeCommands.ImportRtf, token, copy);
            PageResult? page = null;
            if (templateMode)
            {
                if (opening is not ["template"])
                    throw new EditorException(EditorErrors.TemplateModeNotVerified);
            }
            else
                page = await bridge.Page(editor.Pid, token) ?? ParsePage(opening);
            sessions.Add(id, new Session(id, source, copy, digest, editable, editor, templateMode, editable));
            return new OpenResult(id, Path.GetFileName(copy), EditorSettings.Version, editable, digest, page,
                templateMode ? "template" : "document", opening.Length == 3 ? opening[2] : null);
        }
        catch
        {
            editor.AbortStartup();
            editor.Dispose();
            throw;
        }
    }

    private static void WriteCapitalizationProfile(string profile, CapitalizationState capitalization)
    {
        XElement Property(string key, string value) =>
            new("property", new XAttribute("name", key), new XAttribute("description", key + " Initial Value"), value);
        XElement Group(string key, params XElement[] properties) =>
            new("propertyGroup", new XAttribute("name", key), new XAttribute("description", "Açılış Değerleri"), properties);

        // Native UYAP 5.4.20's startup preferences schema. Seed only these two options.
        var native = new XDocument(new XElement("properties",
            Group("initValues", Property("FirstLetterUpperCase", capitalization.FirstLetterConfigured ? EditorWireValues.True : EditorWireValues.False),
                Property("ToUpperCase", capitalization.UppercaseConfigured ? EditorWireValues.True : EditorWireValues.False)),
            Group("toolbarMenuValues"), Group("personalMenuValues")));
        var folder = Directory.CreateDirectory(Path.Combine(profile, EditorProfilePaths.SettingsFolder)).FullName;
        using var output = new FileStream(Path.Combine(folder, "acilisDegerleri.xml"), FileMode.CreateNew, FileAccess.Write);
        native.Save(output);
    }

    private static void ValidateImportUsf(string file)
    {
        try
        {
            using var document = ZipFile.OpenRead(file);
            if (document.GetEntry(UdfEntries.Content) != null)
                return;
        }
        catch (InvalidDataException) { }
        throw new EditorException(EditorErrors.UsfFormatInvalid);
    }

    private static void ValidateImportRtf(string file)
    {
        using var stream = File.OpenRead(file);
        Span<byte> prefix = stackalloc byte[5];
        if (stream.ReadAtLeast(prefix, prefix.Length, false) != prefix.Length ||
            !prefix.SequenceEqual("{\\rtf"u8))
            throw new EditorException(EditorErrors.RtfFormatInvalid);
    }

    private static void ValidateImportXml(string file)
    {
        using var stream = File.OpenRead(file);
        try
        {
            var options = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = EditorLimits.MaxXmlCharacters
            };
            using var xml = System.Xml.XmlReader.Create(stream, options);
            if (xml.MoveToContent() != System.Xml.XmlNodeType.Element || xml.Name != "template")
                throw new EditorException(EditorErrors.XmlNotNativeDocument);
            while (xml.Read())
            {
            }
        }
        catch (System.Xml.XmlException)
        {
            throw new EditorException(EditorErrors.XmlFormatInvalid);
        }
    }

    private void CheckSessions()
    {
        foreach (var id in sessions.Where(pair => pair.Value.Editor.Exited).Select(pair => pair.Key).ToArray())
        {
            sessions[id].Editor.Dispose();
            sessions.Remove(id);
        }
        if (sessions.Count >= 4)
            throw new EditorException(EditorErrors.SessionLimit);
    }

    internal Task<EditModeResult> EditMode(string id, bool? editable, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        if (editable == true && !session.EditingAllowed)
            throw new EditorException(EditorErrors.ReadOnlySession);
        var state = await session.Editor.Call(EditorBridgeCommands.EditMode, token, editable switch
        {
            true => "set-writable",
            false => "set-read-only",
            null => "",
        });
        if (state.Length != 2 || state.Any(value => value is not (EditorWireValues.True or EditorWireValues.False)))
            throw new EditorException(EditorErrors.EditStateResponseInvalid);
        bool nativeEditable = state[0] == EditorWireValues.True;
        if (editable.HasValue)
        {
            if (nativeEditable != editable.Value)
                throw new EditorException(EditorErrors.EditModeUnverified);
            session = session with
            {
                Editable = editable.Value
            };
            sessions[id] = session;
        }
        return new EditModeResult(session.Editable && nativeEditable, nativeEditable,
            session.EditingAllowed && state[1] == EditorWireValues.True);
    }, token);

    internal Task<CloseResult> Close(string id, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        await session.Editor.Call(EditorBridgeCommands.Close, token);
        if (!session.Editor.Exited && Desktop.Windows(session.Editor.Pid).Any(window =>
                window.Title.Contains("Editörü v" + EditorSettings.Version + " -", StringComparison.Ordinal)))
            throw new EditorException(EditorErrors.CloseUnverified);
        sessions.Remove(id);
        session.Editor.Dispose();
        return new CloseResult(id, true);
    }, token);

    private Session Get(string id)
    {
        if (!sessions.TryGetValue(id, out var session) || session.Editor.Exited)
            throw new EditorException(EditorErrors.DocumentUnknown);
        return session;
    }

    private async Task<T> Exclusive<T>(Func<Task<T>> operation, CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            return await operation();
        }
        finally { gate.Release(); }
    }

    private static void RequireEditable(Session session)
    {
        if (!session.Editable)
            throw new EditorException(EditorErrors.ReadOnlySession);
    }

    private static void PermitEditing(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var entries = zip.Entries.Select(entry => entry.FullName).ToArray();
        if (entries.Count(name => name == UdfEntries.Content) != 1 ||
            entries.Count(name => name == "comments.xml") > 1 ||
            entries.Any(name => name is not (UdfEntries.Content or "comments.xml")))
            throw new EditorException(EditorErrors.UdfEditUnverified);
    }

    private static bool OriginalUnchanged(Session session)
    {
        if (session.Original == null)
            return true;
        try
        {
            return LocalFilePaths.Sha256(session.Original) == session.Digest;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static PageResult ParsePage(string[] response) =>
        new(ReadNumber(response[0]), ReadNumber(response[1]), "Swing status bar");

    private static int ReadNumber(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private static string WriteNumber(int value) => value.ToString(CultureInfo.InvariantCulture);

    public void Dispose()
    {
        foreach (var session in sessions.Values)
            session.Editor.Dispose();
        gate.Dispose();
    }
}
