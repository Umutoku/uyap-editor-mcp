using System.IO.Compression;
using UglyToad.PdfPig;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    private static readonly HashSet<string> Formats =
        new(["doc", "odt", "rtf", "txt", "xml", "usf", "bmp", "jpg", "png", "tif", "tiff", "html", "ps", "xsl", "svg"],
            StringComparer.Ordinal);

    internal Task<PdfResult> ExportPdf(string id, string output, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        var target = settings.Output(output, ".pdf");
        var pending = TemporaryOutput(target, ".pdf");
        EditorProcess? snapshotEditor = null;
        string? snapshot = null;
        try
        {
            var position = session.TemplateMode ? null : ParsePage(await session.Editor.Call(EditorBridgeCommands.Page, token));
            var source = await session.Editor.Call(EditorBridgeCommands.PdfSourceChanged, token);
            if (source is not ["true"] and not ["false"])
                throw new EditorFailure(EditorErrors.PdfSourceStateInvalid);
            if (source[0] == "true")
            {
                // The ribbon can reuse the archive it opened. Give the native ribbon a fresh
                // copy of the live UDF while leaving the original window and its undo state intact.
                var profile = Path.Combine(settings.StateDir, id, "pdf-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(profile);
                snapshot = Path.Combine(profile, "current.udf");
                await session.Editor.Call(EditorBridgeCommands.SnapshotUdf, token, snapshot);
                snapshotEditor = new EditorProcess(settings, profile, snapshot, createBlank: false, session.TemplateMode);
                await snapshotEditor.Call(EditorBridgeCommands.Ready, token);
            }
            var editor = snapshotEditor ?? session.Editor;
            var command = await editor.Call(EditorBridgeCommands.Pdf, token, pending);
            var pdf = await ValidateOutput(pending, stream =>
            {
                using var file = PdfDocument.Open(stream, new ParsingOptions { UseLenientParsing = false });
                var pages = file.NumberOfPages;
                if (pages < 1 || (position != null && pages != position.PageCount))
                    throw new InvalidDataException("PDF page count differs from the editor.");
                for (var i = 1; i <= pages; i++)
                    _ = file.GetPage(i);
                return pages;
            }, token);
            token.ThrowIfCancellationRequested();
            EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
            Publish(pending, target);
            return new PdfResult(target, pdf.Value, pdf.Bytes, pdf.Digest, command[0], OriginalUnchanged(session));
        }
        finally
        {
            if (snapshotEditor != null)
            {
                try
                {
                    await snapshotEditor.Call(EditorBridgeCommands.Close, CancellationToken.None);
                }
                catch (EditorFailure)
                {
                    snapshotEditor.AbortStartup();
                }
                finally
                {
                    snapshotEditor.Dispose();
                }
            }
            DeleteTemporaryOutput(pending, snapshotEditor == null && session.Editor.IsUncertain);
            if (snapshot != null)
                DeleteTemporaryOutput(snapshot, session.Editor.IsUncertain);
        }
    }, token);

    internal Task<SavedCopy> SaveCopy(string id, string output, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        var target = settings.Output(output, ".udf");
        var stage = Path.Combine(Path.GetDirectoryName(session.Staged)!, Guid.NewGuid().ToString("N") + ".udf");
        var pending = TemporaryOutput(target, ".udf");
        try
        {
            await session.Editor.Call(EditorBridgeCommands.SaveUdf, token, stage);
            var result = await ValidateOutput(stage, stream =>
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                if (zip.GetEntry("content.xml") == null)
                    throw new InvalidDataException("The UDF content member is missing.");
                return true;
            }, token);
            token.ThrowIfCancellationRequested();
            File.Copy(stage, pending, overwrite: false);
            EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
            Publish(pending, target);
            await session.Editor.Call(EditorBridgeCommands.MarkSaved, CancellationToken.None);
            return new SavedCopy(target, result.Bytes, result.Digest);
        }
        finally
        {
            DeleteTemporaryOutput(pending, uncertain: false);
        }
    }, token);

    internal Task<FormatExport> Export(string id, string output, CancellationToken token, bool htmlPageBreaks = false) =>
        Exclusive(async () =>
    {
        var session = Get(id);
        var format = Path.GetExtension(output).TrimStart('.').ToLowerInvariant();
        if (!Formats.Contains(format))
            throw new EditorFailure(EditorErrors.FormatUnsupported);
        if (htmlPageBreaks && format != "html")
            throw new EditorFailure(EditorErrors.FormatOptionInvalid);
        var target = settings.Output(output, "." + format);
        var nativeFormat = format == "tif" ? "tiff" : format;
        if (htmlPageBreaks)
            nativeFormat = "html-with-breaks";
        var pending = TemporaryOutput(target, format == "tif" ? ".tiff" : "." + format);
        try
        {
            var verb = format is "rtf" or "html" or "ps" or "xsl" or "svg" ? "render_export" : "export";
            var command = format == "doc"
                ? await ExportDoc(session, pending, token)
                : await session.Editor.Call(verb, token, pending, nativeFormat);
            var result = await ValidateOutput(pending, stream => ValidateFormat(stream, format), token);
            token.ThrowIfCancellationRequested();
            EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
            Publish(pending, target);
            return new FormatExport(target, format, result.Bytes, result.Digest, command[0]);
        }
        finally
        {
            DeleteTemporaryOutput(pending, session.Editor.IsUncertain);
        }
    }, token);

    private async Task<string[]> ExportDoc(Session session, string pending, CancellationToken token)
    {
        try
        {
            return await session.Editor.Call(EditorBridgeCommands.Export, token, pending, "doc");
        }
        catch (EditorFailure error) when (error.Code == EditorErrorCodes.DocOfficeRuntimeUnavailable)
        {
            // Word is an optional local compatibility path after UYAP's native Office preflight.
            // Only hand it a validated RTF generated from the managed document.
            var rtf = TemporaryOutput(pending, ".rtf");
            try
            {
                await session.Editor.Call(EditorBridgeCommands.RenderExport, token, rtf, "rtf");
                await ValidateOutput(rtf, stream => ValidateFormat(stream, "rtf"), token);
                await WordDocConverter.Convert(rtf, pending, token);
                return [WordDocConverter.Command];
            }
            finally
            {
                DeleteTemporaryOutput(rtf, session.Editor.IsUncertain);
            }
        }
    }

    internal async Task<BatchResult> Batch(BatchFile[] files, CancellationToken token)
    {
        if (files.Length is < 1 or > 8)
            throw new EditorFailure(EditorErrors.BatchSizeInvalid);
        List<BatchItem> results = [];
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file.Path);
            string? id = null;
            string? published = null;
            string? error = null;
            try
            {
                var document = await Open(file.Path, editable: false, token);
                id = document.DocumentId;
                published = file.OutputPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? (await ExportPdf(id, file.OutputPath, token)).Path
                    : (await Export(id, file.OutputPath, token)).Path;
            }
            catch (EditorFailure failed) { error = failed.Code; }
            catch (Exception failed) when (failed is IOException or UnauthorizedAccessException) { error = "LOCAL_IO_ERROR"; }
            finally
            {
                if (id != null)
                {
                    try
                    {
                        await Close(id, CancellationToken.None);
                        id = null;
                    }
                    catch (EditorFailure failed) { error ??= failed.Code; }
                }
            }
            results.Add(new BatchItem(name, published != null, published, error, id));
        }
        return new BatchResult(results.Count(item => item.Succeeded), results.ToArray());
    }

    internal async Task<AttachmentPreparation> PrepareAttachment(string id, string action, string output,
        CancellationToken token)
    {
        var format = AttachmentFormat(action);
        if (!string.Equals(Path.GetExtension(output), "." + format, StringComparison.OrdinalIgnoreCase))
            throw new EditorFailure(EditorErrors.AttachmentFormatMismatch);

        FormatExport artifact;
        if (format == "udf")
            artifact = await ExportAttachmentUdf(id, output, token);
        else if (format == "pdf")
        {
            var pdf = await ExportPdf(id, output, token);
            artifact = new FormatExport(pdf.Path, format, pdf.Bytes, pdf.Sha256, pdf.EditorCommand);
        }
        else
            artifact = await Export(id, output, token);
        return new AttachmentPreparation(action, format, artifact.Path, artifact.Bytes, artifact.Sha256,
            artifact.EditorCommand, EditorWorkflowStatuses.UserActionRequired);
    }

    private static string AttachmentFormat(string action) => action switch
    {
        EditorActionIds.AttachUdf => "udf",
        EditorActionIds.AttachDoc => "doc",
        EditorActionIds.AttachOdt => "odt",
        EditorActionIds.AttachPdf => "pdf",
        _ => throw new EditorFailure(EditorErrors.AttachmentActionUnavailable),
    };

    private Task<FormatExport> ExportAttachmentUdf(string id, string output, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            var target = settings.Output(output, ".udf");
            var pending = TemporaryOutput(target, ".udf");
            try
            {
                var state = await session.Editor.Call(EditorBridgeCommands.PdfSourceChanged, token);
                if (state is not ["true"] and not ["false"])
                    throw new EditorFailure(EditorErrors.DocumentStateResponseInvalid);
                string command;
                if (state[0] == "false" && File.Exists(session.Staged)
                    && string.Equals(Path.GetExtension(session.Staged), ".udf", StringComparison.OrdinalIgnoreCase))
                {
                    EditorSettings.RequireExistingPlainPath(session.Staged);
                    File.Copy(session.Staged, pending, overwrite: false);
                    command = "copy:managed-udf";
                }
                else
                {
                    await session.Editor.Call(EditorBridgeCommands.SnapshotUdf, token, pending);
                    command = "snapshot:native-udf";
                }
                var result = await ValidateOutput(pending, stream =>
                {
                    using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                    if (zip.GetEntry("content.xml") == null)
                        throw new InvalidDataException("The UDF content member is missing.");
                    return true;
                }, token);
                token.ThrowIfCancellationRequested();
                EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
                Publish(pending, target);
                return new FormatExport(target, "udf", result.Bytes, result.Digest, command);
            }
            finally
            {
                DeleteTemporaryOutput(pending, session.Editor.IsUncertain);
            }
        }, token);
}
