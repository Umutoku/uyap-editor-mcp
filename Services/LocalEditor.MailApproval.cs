using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    private async Task<ApprovalReview> PrepareMailApproval(ApprovalRequest request, CancellationToken token)
    {
        var format = AttachmentFormat(request.Action);
        if (request.Documents is not [var document])
            throw new EditorFailure(EditorErrors.MailApprovalDocumentsInvalid);

        string userStep = ApprovalText.MailApprovalStep;
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(document.OutputPath))
            missing.Add("documents[0].outputPath");
        if (string.IsNullOrWhiteSpace(request.DraftPath))
            missing.Add("draftPath");
        if (request.Recipients is not { Length: > 0 })
            missing.Add("recipients");
        if (string.IsNullOrWhiteSpace(request.Subject))
            missing.Add("subject");
        if (request.Message == null)
            missing.Add("message");
        if (missing.Count != 0)
            return new ApprovalReview(request.Action, EditorWorkflowStatuses.InputRequired, null, [], [], missing.ToArray(), userStep);

        string[] recipients = ValidateMailRecipients(request.Recipients!);
        string subject = request.Subject!;
        string message = request.Message!;
        if (subject.Length > 512 || subject.Any(char.IsControl))
            throw new EditorFailure(EditorErrors.MailSubjectInvalid);
        if (message.Length > 16_384 || message.Any(value => char.IsControl(value) && value is not ('\t' or '\r' or '\n')))
            throw new EditorFailure(EditorErrors.MailMessageInvalid);

        var attachmentTarget = documents.Output(document.OutputPath!, "." + format);
        var draftTarget = documents.Output(request.DraftPath!, FileExtensions.Eml);
        var stagedAttachment = DocumentOutputRepository.TemporaryOutput(attachmentTarget, "." + format);
        var stagedDraft = DocumentOutputRepository.TemporaryOutput(draftTarget, FileExtensions.Eml);
        string attachmentName = Path.GetFileName(attachmentTarget);

        try
        {
            var attachment = await PrepareAttachment(document.DocumentId, request.Action, stagedAttachment, token);
            WriteMailDraft(stagedDraft, stagedAttachment, attachment, recipients, subject, message, attachmentName, token);
            var draftDigest = LocalFilePaths.Sha256(stagedDraft);
            var draftSize = new FileInfo(stagedDraft).Length;
            token.ThrowIfCancellationRequested();

            // Both outputs are ready before moving either to its requested name.
            documents.Output(attachmentTarget, "." + format);
            documents.Output(draftTarget, FileExtensions.Eml);
            DocumentOutputRepository.Publish(stagedAttachment, attachmentTarget);
            try
            {
                DocumentOutputRepository.Publish(stagedDraft, draftTarget);
            }
            catch (Exception error) when (error is EditorFailure or IOException or UnauthorizedAccessException)
            {
                throw new EditorFailure(EditorErrors.MailDraftPublishFailed, attachmentTarget);
            }

            return new ApprovalReview(request.Action, EditorWorkflowStatuses.AwaitingUserConfirmation, null,
                [new ApprovalArtifact(attachmentTarget, format, attachment.Bytes, attachment.Sha256),
                 new ApprovalArtifact(draftTarget, DocumentFormats.Eml, draftSize, draftDigest)],
                [new ApprovalItem("Alıcılar", string.Join(", ", recipients)), new ApprovalItem("Konu", subject),
                 new ApprovalItem("İleti", message), new ApprovalItem("Ek", attachmentName),
                 new ApprovalItem("Ek hazırlama yöntemi", attachment.EditorCommand)], [], userStep);
        }
        finally
        {
            DocumentOutputRepository.DeleteTemporaryOutput(stagedAttachment, uncertain: false);
            DocumentOutputRepository.DeleteTemporaryOutput(stagedDraft, uncertain: false);
        }
    }

    private static string[] ValidateMailRecipients(string[] recipients)
    {
        if (recipients.Length > 20 || recipients.Any(address => !IsPlainMailAddress(address)))
            throw new EditorFailure(EditorErrors.MailRecipientInvalid);
        return recipients;
    }

    private static bool IsPlainMailAddress(string? address)
    {
        if (address is not { Length: > 0 and <= 254 } ||
            address.Any(value => value < '!' || value > '~' || "<>,:;\"\\()[]".Contains(value)))
            return false;
        if (address.IndexOf('@') is < 1 or > 64 ||
            !MailAddress.TryCreate(address, out var parsed) ||
            !string.Equals(parsed.Address, address, StringComparison.Ordinal))
            return false;
        return parsed.Host.Split('.').All(label => label.Length is > 0 and <= 63 &&
            char.IsAsciiLetterOrDigit(label[0]) && char.IsAsciiLetterOrDigit(label[^1]) &&
            label.All(value => char.IsAsciiLetterOrDigit(value) || value == '-'));
    }

    private static void WriteMailDraft(string path, string attachmentPath, AttachmentPreparation attachment,
        string[] recipients, string subject, string message, string attachmentName, CancellationToken token)
    {
        LocalFilePaths.RequireExistingPlainPath(attachmentPath);
        using var source = new FileStream(attachmentPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (source.Length != attachment.Bytes ||
            !string.Equals(Convert.ToHexString(SHA256.HashData(source)), attachment.Sha256, StringComparison.Ordinal))
            throw new EditorFailure(EditorErrors.MailAttachmentChanged);
        source.Position = 0;
        string boundary = "uyap-" + Guid.NewGuid().ToString("N");
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(output, Encoding.ASCII) { NewLine = "\r\n" };
        writer.WriteLine("X-Unsent: 1");
        writer.WriteLine($"To: {string.Join(",\r\n ", recipients)}");
        writer.WriteLine($"Subject: {EncodeMailSubject(subject)}");
        writer.WriteLine("MIME-Version: 1.0");
        writer.WriteLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        writer.WriteLine();
        writer.WriteLine($"--{boundary}");
        writer.WriteLine("Content-Type: text/plain; charset=utf-8");
        writer.WriteLine("Content-Transfer-Encoding: base64");
        writer.WriteLine();
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(message.ReplaceLineEndings("\r\n")));
        WriteMailBase64(writer, body, token);
        writer.WriteLine($"--{boundary}");
        writer.WriteLine("Content-Type: " + (attachment.Format switch
        {
            DocumentFormats.Doc => "application/msword",
            DocumentFormats.Odt => "application/vnd.oasis.opendocument.text",
            DocumentFormats.Pdf => "application/pdf",
            _ => "application/octet-stream",
        }));
        WriteMailFilename(writer, attachmentName);
        writer.WriteLine("Content-Transfer-Encoding: base64");
        writer.WriteLine();
        WriteMailBase64(writer, source, token);
        writer.WriteLine($"--{boundary}--");
    }

    private static string EncodeMailSubject(string subject)
    {
        // An RFC 2047 encoded word has a 75-character limit, including its delimiters.
        var words = new List<string>();
        Span<byte> chunk = stackalloc byte[42];
        int used = 0;
        foreach (Rune rune in subject.EnumerateRunes())
        {
            if (used + rune.Utf8SequenceLength > chunk.Length)
            {
                words.Add("=?UTF-8?B?" + Convert.ToBase64String(chunk[..used]) + "?=");
                used = 0;
            }
            used += rune.EncodeToUtf8(chunk[used..]);
        }
        if (used != 0)
            words.Add("=?UTF-8?B?" + Convert.ToBase64String(chunk[..used]) + "?=");
        return string.Join("\r\n ", words);
    }

    private static void WriteMailFilename(StreamWriter writer, string filename)
    {
        // RFC 2231 continuations preserve Turkish and other UTF-8 names without long header lines.
        var encoded = Encoding.UTF8.GetBytes(filename);
        writer.Write("Content-Disposition: attachment");
        for (int offset = 0; offset < encoded.Length; offset += 16)
        {
            writer.WriteLine(';');
            writer.Write(" filename*" + (offset / 16).ToString(CultureInfo.InvariantCulture) + "*=");
            if (offset == 0)
                writer.Write("utf-8''");
            for (int i = offset; i < Math.Min(offset + 16, encoded.Length); i++)
            {
                writer.Write('%');
                writer.Write(encoded[i].ToString("X2", CultureInfo.InvariantCulture));
            }
        }
        writer.WriteLine();
    }

    private static void WriteMailBase64(StreamWriter writer, Stream source, CancellationToken token)
    {
        var buffer = new byte[57 * 128];
        while (true)
        {
            token.ThrowIfCancellationRequested();
            int count = source.ReadAtLeast(buffer, buffer.Length, false);
            if (count == 0)
                return;
            string encoded = Convert.ToBase64String(buffer, 0, count);
            for (int offset = 0; offset < encoded.Length; offset += 76)
                writer.WriteLine(encoded.AsSpan(offset, Math.Min(76, encoded.Length - offset)));
        }
    }
}
