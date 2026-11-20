using System.IO.Compression;

namespace UyapEditorMcp;

internal sealed partial class LocalEditor
{
    internal Task<NativeBackups> ListBackups(int offset, int limit, CancellationToken token) => Exclusive(() =>
    {
        if (offset < 0 || limit is < 1 or > 50)
            throw new EditorFailure(EditorErrors.CatalogRangeInvalid);
        var paths = settings.BackupFolders()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.udf"))
            .Select(path => new FileInfo(path))
            .Where(file => EditorSettings.IsBackupName(file.Name) && file.Exists &&
                (file.Attributes & FileAttributes.ReparsePoint) == 0 && file.Length is > 0 and <= 50 * 1024 * 1024)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
            .Skip(offset).Take(limit + 1).ToArray();
        var backups = new List<NativeBackup>();
        foreach (var file in paths.Take(limit))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                backups.Add(new NativeBackup(file.FullName, file.Name[1..], file.Length, file.LastWriteTimeUtc,
                    EditorSettings.Sha256(file.FullName)));
            }
            catch (FileNotFoundException)
            {
                // UYAP may consume one of its backups while listing.
            }
        }
        return Task.FromResult(new NativeBackups(offset, backups.ToArray(),
            paths.Length > limit ? offset + limit : null));
    }, token);

    internal Task<BackupRecovery> RestoreBackup(string backupPath, string expectedSha256, string output,
        CancellationToken token) => Exclusive(async () =>
    {
        var source = settings.BackupSource(backupPath);
        var target = settings.Output(output, ".udf");
        var pending = TemporaryOutput(target, ".udf");
        var profile = Path.Combine(settings.StateDir, "recover-" + Guid.NewGuid().ToString("N"));
        var backupFolder = Path.Combine(profile, ".uki", "yedekler");
        Directory.CreateDirectory(backupFolder);
        var staged = Path.Combine(backupFolder, "~MCP-Recovery.udf");
        EditorProcess? editor = null;
        try
        {
            // Hold the source open against writes while copying an autosave snapshot.
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length is <= 0 or > 50 * 1024 * 1024)
                    throw new EditorFailure(EditorErrors.BackupSizeInvalid);
                using var copy = new FileStream(staged + ".pending", FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await input.CopyToAsync(copy, token);
            }
            var digest = EditorSettings.Sha256(staged + ".pending");
            if (!string.Equals(digest, expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new EditorFailure(EditorErrors.BackupStale);
            try
            {
                using var archive = ZipFile.OpenRead(staged + ".pending");
                var member = archive.GetEntry("content.xml");
                if (member == null || archive.Entries.Count(entry => entry.FullName == "content.xml") != 1)
                    throw new InvalidDataException("Missing or ambiguous UDF content.");
                using var document = member.Open();
                if (!ValidateXml(document, "template", ""))
                    throw new InvalidDataException("Invalid UDF document root.");
            }
            catch (InvalidDataException)
            {
                throw new EditorFailure(EditorErrors.BackupUdfInvalid);
            }

            var blank = Path.Combine(profile, ".uki", "isimsiz.UDF");
            editor = new EditorProcess(settings, profile, blank, createBlank: true);
            await editor.Call(EditorBridgeCommands.Ready, token);
            File.Move(staged + ".pending", staged);
            var command = await editor.Call(EditorBridgeCommands.RestoreBackup, token, pending);
            if (command is not [var editorCommand])
                throw new EditorFailure(EditorErrors.BackupRecoveryResponseInvalid);
            var result = await ValidateOutput(pending, _ => true, token);
            if (!string.Equals(result.Digest, digest, StringComparison.Ordinal))
                throw new EditorFailure(EditorErrors.BackupOutputMismatch);
            token.ThrowIfCancellationRequested();
            EditorSettings.RequireExistingPlainPath(Path.GetDirectoryName(target)!);
            Publish(pending, target);
            return new BackupRecovery(new SavedCopy(target, result.Bytes, result.Digest), editorCommand);
        }
        finally
        {
            if (editor != null)
            {
                editor.AbortStartup();
                editor.Dispose();
            }
            DeleteTemporaryOutput(pending, uncertain: false);
            DeleteTemporaryOutput(staged, uncertain: false);
            DeleteTemporaryOutput(staged + ".pending", uncertain: false);
        }
    }, token);
}
