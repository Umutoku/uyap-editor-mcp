using System.IO.Compression;
using UglyToad.PdfPig;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    private static readonly HashSet<string> Formats =
        new([
            DocumentFormats.Doc, DocumentFormats.Odt, DocumentFormats.Rtf,
            DocumentFormats.Text, DocumentFormats.Xml, DocumentFormats.Usf,
            DocumentFormats.Bmp, DocumentFormats.JpegShort, DocumentFormats.Png,
            DocumentFormats.TiffShort, DocumentFormats.Tiff, DocumentFormats.Html,
            DocumentFormats.PostScript, DocumentFormats.Xsl, DocumentFormats.Svg,
        ],
            StringComparer.Ordinal);

    internal Task<PdfResult> ExportPdf(string id, string output, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        var target = documents.Output(output, FileExtensions.Pdf);
        var pending = DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Pdf);
        EditorProcess? snapshotEditor = null;
        string? snapshot = null;
        try
        {
            var position = session.TemplateMode ? null : ParsePage(await session.Editor.Call(EditorBridgeCommands.Page, token));
            var hasSourceChanges = await session.Editor.HasSourceChanges(EditorErrors.PdfSourceStateInvalid, token);
            if (hasSourceChanges)
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
            var pdf = await DocumentOutputRepository.ValidateOutput(pending, stream =>
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
            DocumentOutputRepository.Publish(pending, target);
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
                catch (EditorException)
                {
                    snapshotEditor.AbortStartup();
                }
                finally
                {
                    snapshotEditor.Dispose();
                }
            }
            DocumentOutputRepository.DeleteTemporaryOutput(pending, snapshotEditor == null && session.Editor.IsUncertain);
            if (snapshot != null)
                DocumentOutputRepository.DeleteTemporaryOutput(snapshot, session.Editor.IsUncertain);
        }
    }, token);

    internal Task<SavedCopy> SaveCopy(string id, string output, CancellationToken token) => Exclusive(async () =>
    {
        var session = Get(id);
        RequireEditable(session);
        var target = documents.Output(output, FileExtensions.Udf);
        var stage = Path.Combine(Path.GetDirectoryName(session.Staged)!, Guid.NewGuid().ToString("N") + FileExtensions.Udf);
        var pending = DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Udf);
        try
        {
            await session.Editor.Call(EditorBridgeCommands.SaveUdf, token, stage);
            var result = await DocumentOutputRepository.ValidateOutput(stage, stream =>
            {
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                if (zip.GetEntry(UdfEntries.Content) == null)
                    throw new InvalidDataException("The UDF content member is missing.");
                return true;
            }, token);
            token.ThrowIfCancellationRequested();
            File.Copy(stage, pending, overwrite: false);
            DocumentOutputRepository.Publish(pending, target);
            await session.Editor.Call(EditorBridgeCommands.MarkSaved, CancellationToken.None);
            return new SavedCopy(target, result.Bytes, result.Digest);
        }
        finally
        {
            DocumentOutputRepository.DeleteTemporaryOutput(pending, uncertain: false);
        }
    }, token);

    internal Task<FormatExport> Export(string id, string output, CancellationToken token, bool htmlPageBreaks = false) =>
        Exclusive(async () =>
    {
        var session = Get(id);
        var format = Path.GetExtension(output).TrimStart('.').ToLowerInvariant();
        if (!Formats.Contains(format))
            throw new EditorException(EditorErrors.FormatUnsupported);
        if (htmlPageBreaks && format != DocumentFormats.Html)
            throw new EditorException(EditorErrors.FormatOptionInvalid);
        var target = documents.Output(output, "." + format);
        var nativeFormat = format == DocumentFormats.TiffShort ? DocumentFormats.Tiff : format;
        if (htmlPageBreaks)
            nativeFormat = "html-with-breaks";
        var pending = DocumentOutputRepository.TemporaryOutput(target, format == DocumentFormats.TiffShort ? FileExtensions.Tiff : "." + format);
        try
        {
            var verb = format is DocumentFormats.Rtf or DocumentFormats.Html or DocumentFormats.PostScript or DocumentFormats.Xsl or DocumentFormats.Svg ? "render_export" : "export";
            var command = format == DocumentFormats.Doc
                ? await ExportDoc(session, pending, token)
                : await session.Editor.Call(verb, token, pending, nativeFormat);
            var result = await DocumentOutputRepository.ValidateOutput(pending, stream => DocumentOutputRepository.ValidateFormat(stream, format), token);
            token.ThrowIfCancellationRequested();
            DocumentOutputRepository.Publish(pending, target);
            return new FormatExport(target, format, result.Bytes, result.Digest, command[0]);
        }
        finally
        {
            DocumentOutputRepository.DeleteTemporaryOutput(pending, session.Editor.IsUncertain);
        }
    }, token);

    private async Task<string[]> ExportDoc(Session session, string pending, CancellationToken token)
    {
        try
        {
            return await session.Editor.Call(EditorBridgeCommands.Export, token, pending, DocumentFormats.Doc);
        }
        catch (EditorException error) when (!session.Editor.IsUncertain &&
            error.Code is EditorErrorCodes.DocOfficeRuntimeUnavailable or EditorErrorCodes.SaveDialogTimeout)
        {
            // Word is an optional local compatibility path after UYAP's native Office preflight.
            // Only hand it a validated RTF generated from the managed document.
            var rtf = DocumentOutputRepository.TemporaryOutput(pending, FileExtensions.Rtf);
            try
            {
                await session.Editor.Call(EditorBridgeCommands.RenderExport, token, rtf, DocumentFormats.Rtf);
                await DocumentOutputRepository.ValidateOutput(rtf, stream => DocumentOutputRepository.ValidateFormat(stream, DocumentFormats.Rtf), token);
                await WordDocConverter.Convert(rtf, pending, token);
                return [WordDocConverter.Command];
            }
            finally
            {
                DocumentOutputRepository.DeleteTemporaryOutput(rtf, session.Editor.IsUncertain);
            }
        }
    }

    internal async Task<BatchResult> Batch(BatchFile[] files, CancellationToken token)
    {
        if (files.Length is < 1 or > 8)
            throw new EditorException(EditorErrors.BatchSizeInvalid);
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
                published = file.OutputPath.EndsWith(FileExtensions.Pdf, StringComparison.OrdinalIgnoreCase)
                    ? (await ExportPdf(id, file.OutputPath, token)).Path
                    : (await Export(id, file.OutputPath, token)).Path;
            }
            catch (EditorException failed) { error = failed.Code; }
            catch (Exception failed) when (failed is IOException or UnauthorizedAccessException) { error = EditorErrorCodes.LocalIoError; }
            finally
            {
                if (id != null)
                {
                    try
                    {
                        await Close(id, CancellationToken.None);
                        id = null;
                    }
                    catch (EditorException failed) { error ??= failed.Code; }
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
            throw new EditorException(EditorErrors.AttachmentFormatMismatch);

        FormatExport artifact;
        if (format == DocumentFormats.Udf)
            artifact = await ExportAttachmentUdf(id, output, token);
        else if (format == DocumentFormats.Pdf)
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
        EditorActionIds.AttachUdf => DocumentFormats.Udf,
        EditorActionIds.AttachDoc => DocumentFormats.Doc,
        EditorActionIds.AttachOdt => DocumentFormats.Odt,
        EditorActionIds.AttachPdf => DocumentFormats.Pdf,
        _ => throw new EditorException(EditorErrors.AttachmentActionUnavailable),
    };

    private Task<FormatExport> ExportAttachmentUdf(string id, string output, CancellationToken token) =>
        Exclusive(async () =>
        {
            var session = Get(id);
            var target = documents.Output(output, FileExtensions.Udf);
            var pending = DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Udf);
            try
            {
                var hasSourceChanges = await session.Editor.HasSourceChanges(
                    EditorErrors.DocumentStateResponseInvalid, token);

                string command;
                if (!hasSourceChanges && File.Exists(session.Staged)
                    && string.Equals(Path.GetExtension(session.Staged), FileExtensions.Udf, StringComparison.OrdinalIgnoreCase))
                {
                    LocalFilePaths.RequireExistingPlainPath(session.Staged);
                    File.Copy(session.Staged, pending, overwrite: false);
                    command = "copy:managed-udf";
                }
                else
                {
                    await session.Editor.Call(EditorBridgeCommands.SnapshotUdf, token, pending);
                    command = "snapshot:native-udf";
                }
                var result = await DocumentOutputRepository.ValidateOutput(pending, stream =>
                {
                    using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                    if (zip.GetEntry(UdfEntries.Content) == null)
                        throw new InvalidDataException("The UDF content member is missing.");
                    return true;
                }, token);
                token.ThrowIfCancellationRequested();
                DocumentOutputRepository.Publish(pending, target);
                return new FormatExport(target, DocumentFormats.Udf, result.Bytes, result.Digest, command);
            }
            finally
            {
                DocumentOutputRepository.DeleteTemporaryOutput(pending, session.Editor.IsUncertain);
            }
        }, token);
}
