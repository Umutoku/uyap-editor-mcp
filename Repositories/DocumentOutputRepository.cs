using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;

namespace UyapEditorMcp;

internal static class DocumentOutputRepository
{
    internal static async Task<(T Value, long Bytes, string Digest)> ValidateOutput<T>(
        string path, Func<Stream, T> parse, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(20))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                if (stream.Length > 0)
                {
                    var result = parse(stream);
                    stream.Position = 0;
                    var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, token);
                    var digest = Convert.ToHexString(hash);
                    return (result, stream.Length, digest);
                }
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UglyToad.PdfPig.Core.PdfDocumentFormatException)
            {
                // The editor writes asynchronously and may have created an unfinished file.
            }
            await Task.Delay(125, token);
        }
        throw new EditorException(EditorErrors.OutputUnverified);
    }

    internal static bool ValidateFormat(Stream stream, string format)
    {
        Span<byte> prefix = stackalloc byte[16];
        var read = stream.Read(prefix);
        prefix = prefix[..read];
        stream.Position = 0;
        var valid = format switch
        {
            DocumentFormats.Odt => ValidateOdt(stream),
            DocumentFormats.Rtf => prefix.StartsWith("{\\rtf"u8),
            DocumentFormats.Html => prefix.StartsWith("<!DOCTYPE html"u8) || prefix.StartsWith("<html"u8),
            DocumentFormats.PostScript => prefix.StartsWith("%!PS-Adobe"u8) && EndsWith(stream, "%%EOF\n"u8),
            DocumentFormats.Xsl => ValidateXml(stream, "root", DocumentXmlNamespaces.Xsl),
            DocumentFormats.Svg => ValidateXml(stream, DocumentFormats.Svg, DocumentXmlNamespaces.Svg),
            DocumentFormats.Doc => stream.Length >= 512 &&
                prefix.StartsWith(new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 }),
            DocumentFormats.Bmp => read >= 6 && prefix.StartsWith("BM"u8) &&
                BinaryPrimitives.ReadUInt32LittleEndian(prefix[2..6]) == stream.Length,
            DocumentFormats.JpegShort => prefix.StartsWith(new byte[] { 0xff, 0xd8, 0xff }) &&
                EndsWith(stream, new byte[] { 0xff, 0xd9 }),
            DocumentFormats.Png => prefix.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }) &&
                EndsWith(stream, new byte[] { 0, 0, 0, 0, 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82 }),
            DocumentFormats.TiffShort or DocumentFormats.Tiff => HasTiffDirectory(stream, prefix),
            DocumentFormats.Xml => ValidateXml(stream),
            DocumentFormats.Usf => prefix.StartsWith("PK"u8) || ValidateXml(stream),
            DocumentFormats.Text => read > 0 && !prefix.Contains((byte)0),
            _ => false,
        };
        if (!valid)
            throw new InvalidDataException("The exported file does not match the chosen document format.");
        return true;
    }

    private static bool EndsWith(Stream stream, ReadOnlySpan<byte> suffix)
    {
        if (stream.Length < suffix.Length)
            return false;
        stream.Seek(-suffix.Length, SeekOrigin.End);
        Span<byte> ending = stackalloc byte[suffix.Length];
        stream.ReadExactly(ending);
        return ending.SequenceEqual(suffix);
    }

    private static bool HasTiffDirectory(Stream stream, ReadOnlySpan<byte> prefix)
    {
        if (prefix.Length < 8)
            return false;
        var littleEndian = prefix.StartsWith(new byte[] { 0x49, 0x49, 0x2a, 0 });
        if (!littleEndian && !prefix.StartsWith(new byte[] { 0x4d, 0x4d, 0, 0x2a }))
            return false;
        var offset = littleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(prefix[4..])
            : BinaryPrimitives.ReadUInt32BigEndian(prefix[4..]);
        if (offset < 8 || (long)offset + 2 > stream.Length)
            return false;
        stream.Position = offset;
        Span<byte> entryCount = stackalloc byte[2];
        stream.ReadExactly(entryCount);
        var count = littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(entryCount)
            : BinaryPrimitives.ReadUInt16BigEndian(entryCount);
        return count > 0 && (long)offset + 2 + count * 12L + 4 <= stream.Length;
    }

    private static bool ValidateOdt(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var mime = zip.GetEntry("mimetype");
        if (mime?.Length is not (>= 20 and <= 150) || zip.GetEntry(UdfEntries.Content) == null)
            return false;
        using var reader = new StreamReader(mime.Open());
        return reader.ReadToEnd() == "application/vnd.oasis.opendocument.text";
    }

    internal static bool ValidateXml(Stream stream, string? root = null, string? namespaceUri = null)
    {
        try
        {
            var settings = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = EditorLimits.MaxXmlCharacters
            };
            using var reader = System.Xml.XmlReader.Create(stream, settings);
            if (root != null && (reader.MoveToContent() != System.Xml.XmlNodeType.Element ||
                reader.LocalName != root || reader.NamespaceURI != namespaceUri))
                return false;
            while (reader.Read())
            {
                // Read to EOF so malformed XML after the root is rejected.
            }
            return true;
        }
        catch (System.Xml.XmlException) { return false; }
    }

    internal static void Publish(string pending, string target)
    {
        LocalFilePaths.RequireExistingPlainPath(Path.GetDirectoryName(target)!);

        try
        {
            File.Move(pending, target, overwrite: false);
        }
        catch (IOException) when (File.Exists(target) || Directory.Exists(target))
        {
            throw new EditorException(EditorErrors.OutputExists);
        }
    }

    internal static string TemporaryOutput(string target, string extension) =>
        Path.Combine(Path.GetDirectoryName(target)!, ".uyap-" + Guid.NewGuid().ToString("N") + extension);

    internal static void DeleteTemporaryOutput(string path, bool uncertain)
    {
        if (uncertain)
            return;
        try
        {
            File.Delete(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // An editor process may still own the staging file during cleanup.
        }
    }
}
