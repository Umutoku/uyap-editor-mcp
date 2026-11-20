using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace UyapEditorMcp;

internal sealed class LocalArchiveService(LocalDocumentRepository documents)
{
    private static readonly UTF8Encoding ArchiveUtf8 = new(false, true);

    internal async Task<LocalArchiveResult> Encrypt7z(string udfPath, string output, string passwordFile, CancellationToken token)
    {
        var source = documents.Input(udfPath, allowOutputRoot: true);
        var target = documents.Output(output, FileExtensions.SevenZip);
        var password = ReadArchivePassword(passwordFile);
        var expectedName = Path.GetFileName(source);
        var pending = DocumentOutputRepository.TemporaryOutput(target, FileExtensions.SevenZip);
        using var original = File.OpenRead(source);
        ValidateArchiveUdf(original);
        try
        {
            await RunSevenZip(["a", "-t7z", "-mhe=on", "-p", "-spd", "--", pending, source], password,
                Stream.Null, EditorLimits.ArchiveListingBytes, token);
            var entry = await InspectEncryptedArchive(pending, password, token);
            if (entry.Name != expectedName || entry.Size != original.Length)
                throw new EditorFailure(EditorErrors.ArchiveContentsUnverified);
            await RunSevenZip(["t", "-t7z", "--", pending], password, Stream.Null, EditorLimits.ArchiveListingBytes, token);
            return PublishArchive(pending, target, DocumentFormats.SevenZip, token);
        }
        finally
        {
            DocumentOutputRepository.DeleteTemporaryOutput(pending, uncertain: false);
        }
    }

    internal async Task<LocalArchiveResult> Extract7z(string archivePath, string output, string passwordFile, CancellationToken token)
    {
        var source = documents.ArchiveInput(archivePath);
        var target = documents.Output(output, FileExtensions.Udf);
        var password = ReadArchivePassword(passwordFile);
        using var archive = File.OpenRead(source);
        var entry = await InspectEncryptedArchive(source, password, token);
        var pending = DocumentOutputRepository.TemporaryOutput(target, FileExtensions.Udf);
        try
        {
            // Stream to our own new file. The archive never supplies any filesystem destination.
            using (var staged = new FileStream(pending, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                await RunSevenZip(["x", "-t7z", "-so", "-bso0", "-spd", "--", source], password,
                    staged, entry.Size, token);
                if (staged.Length != entry.Size)
                    throw new EditorFailure(EditorErrors.ArchiveExtractedSizeMismatch);
                staged.Position = 0;
                ValidateArchiveUdf(staged);
            }
            return PublishArchive(pending, target, DocumentFormats.Udf, token);
        }
        finally
        {
            DocumentOutputRepository.DeleteTemporaryOutput(pending, uncertain: false);
        }
    }

    private string ReadArchivePassword(string path)
    {
        string password;
        try
        {
            password = File.ReadAllText(documents.ArchivePasswordFile(path), ArchiveUtf8);
        }
        catch (DecoderFallbackException)
        {
            throw new EditorFailure(EditorErrors.ArchivePasswordEncodingInvalid);
        }
        if (password.EndsWith('\n'))
            password = password.EndsWith("\r\n", StringComparison.Ordinal) ? password[..^2] : password[..^1];
        if (password.Length is < 1 or > 128 || password.Any(char.IsControl))
            throw new EditorFailure(EditorErrors.ArchivePasswordInvalid);
        return password;
    }

    private static async Task<(string Name, long Size)> InspectEncryptedArchive(string path, string password, CancellationToken token)
    {
        using var listing = new MemoryStream();
        await RunSevenZip(["l", "-t7z", "-slt", "-ba", "--", path], password, listing, EditorLimits.ArchiveListingBytes, token);
        var lines = ArchiveUtf8.GetString(listing.ToArray()).Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        var names = lines.Where(line => line.StartsWith("Path = ", StringComparison.Ordinal)).Select(line => line[7..]).ToArray();
        var sizes = lines.Where(line => line.StartsWith("Size = ", StringComparison.Ordinal)).Select(line => line[7..]).ToArray();
        if (names is not [var name] || name.Contains('/') || name.Contains('\\') || name.Contains(':') ||
            !Path.GetExtension(name).Equals(FileExtensions.Udf, StringComparison.OrdinalIgnoreCase) ||
            sizes is not [var sizeText] || !long.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out var size) ||
            size is <= 0 or > EditorLimits.DocumentBytes || !lines.Contains("Encrypted = +") ||
            !lines.Any(line => line.StartsWith("Method = ", StringComparison.Ordinal) && line.Contains("7zAES", StringComparison.Ordinal)))
            throw new EditorFailure(EditorErrors.ArchiveContentUnsupported);
        return (name, size);
    }

    private static void ValidateArchiveUdf(Stream stream)
    {
        try
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var content = zip.Entries.Where(entry => entry.FullName == UdfEntries.Content).ToArray();
            if (content is [var xml] && xml.Length is > 0 and <= EditorLimits.DocumentBytes)
            {
                using var body = xml.Open();
                if (DocumentOutputRepository.ValidateXml(body))
                    return;
            }
        }
        catch (InvalidDataException) { }
        throw new EditorFailure(EditorErrors.ArchiveUdfInvalid);
    }

    private static LocalArchiveResult PublishArchive(string pending, string target, string format, CancellationToken token)
    {
        var bytes = new FileInfo(pending).Length;
        var digest = LocalFilePaths.Sha256(pending);
        token.ThrowIfCancellationRequested();
        DocumentOutputRepository.Publish(pending, target);
        return new LocalArchiveResult(target, bytes, digest, format);
    }

    private static async Task RunSevenZip(string[] arguments, string password, Stream output, long outputLimit, CancellationToken token)
    {
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe");
        if (!File.Exists(executable))
            throw new EditorFailure(EditorErrors.SevenZipMissing);
        LocalFilePaths.RequireExistingPlainPath(executable);

        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = ArchiveUtf8,
        };
        start.ArgumentList.Add(arguments[0]);
        foreach (var option in new[] { "-sccUTF-8", "-bd", "-bsp0" }.Concat(arguments.Skip(1)))
            start.ArgumentList.Add(option);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(90));
        using var process = Process.Start(start) ?? throw new EditorFailure(EditorErrors.SevenZipStartFailed);
        var errorDrain = process.StandardError.BaseStream.CopyToAsync(Stream.Null, timeout.Token);
        try
        {
            await process.StandardInput.WriteLineAsync(password.AsMemory(), timeout.Token);
            process.StandardInput.Close();
            byte[] buffer = new byte[64 * 1024];
            long length = 0;
            var stdout = process.StandardOutput.BaseStream;
            int count;
            while ((count = await stdout.ReadAsync(buffer, timeout.Token)) != 0)
            {
                length += count;
                if (length > outputLimit)
                    throw new EditorFailure(EditorErrors.ArchiveOutputLimit);
                await output.WriteAsync(buffer.AsMemory(0, count), timeout.Token);
            }
            await process.WaitForExitAsync(timeout.Token);
            await errorDrain;
            if (process.ExitCode != 0)
                throw new EditorFailure(EditorErrors.SevenZipFailed);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new EditorFailure(EditorErrors.SevenZipTimeout);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync(CancellationToken.None);
            }
            try
            {
                await errorDrain;
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
        }
    }
}
