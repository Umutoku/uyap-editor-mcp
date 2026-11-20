namespace UyapEditorMcp;

internal sealed class LocalDocumentRepository(EditorSettings settings)
{
    internal string Input(string path, bool allowOutputRoot = false)
        => ScopedInput(path, FileExtensions.Udf, EditorLimits.DocumentBytes, allowOutputRoot);

    internal string ArchiveInput(string path) => ScopedInput(path, FileExtensions.SevenZip, EditorLimits.ArchiveBytes, true);

    internal string ArchivePasswordFile(string path) => ScopedInput(path, FileExtensions.Text, EditorLimits.ArchivePasswordFileBytes, false);

    private string ScopedInput(string path, string extension, long maxBytes, bool allowOutputRoot)
    {
        string full;
        try
        {
            full = InRoot(path, settings.InputRoot, extension);
        }
        catch (EditorFailure failure) when (allowOutputRoot && failure.Code == EditorErrorCodes.PathOutsideScope)
        {
            full = InRoot(path, settings.OutputRoot, extension);
        }
        LocalFilePaths.RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length > maxBytes)
            throw new EditorFailure(EditorErrors.InputInvalid, extension, maxBytes);
        return full;
    }

    internal string ImportDocument(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (FileExtensions.Xml or FileExtensions.Rtf or FileExtensions.Usf))
            throw new EditorFailure(EditorErrors.ImportFormatUnsupported);
        var full = InRoot(path, settings.InputRoot, extension);
        LocalFilePaths.RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length > EditorLimits.DocumentBytes)
            throw new EditorFailure(EditorErrors.ImportFileInvalid);
        return full;
    }

    internal string Image(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (FileExtensions.Png or FileExtensions.JpegShort or FileExtensions.Jpeg
            or FileExtensions.Bmp or FileExtensions.Gif or FileExtensions.TiffShort or FileExtensions.Tiff))
            throw new EditorFailure(EditorErrors.ImageFormatUnsupported);
        var full = InRoot(path, settings.InputRoot, extension);
        LocalFilePaths.RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length > EditorLimits.ImageBytes)
            throw new EditorFailure(EditorErrors.ImageInvalid);
        return full;
    }

    internal string Output(string path, string extension)
    {
        var full = InRoot(path, settings.OutputRoot, extension);
        LocalFilePaths.RequireExistingPlainPath(Path.GetDirectoryName(full)!);
        if (File.Exists(full) || Directory.Exists(full))
            throw new EditorFailure(EditorErrors.OutputExists);
        return full;
    }

    internal IEnumerable<string> BackupFolders()
    {
        if (settings.BackupRoot != null)
            yield return settings.BackupRoot;
        foreach (var profile in Directory.EnumerateDirectories(settings.StateDir))
        {
            if (!Guid.TryParseExact(Path.GetFileName(profile), "N", out _) ||
                (File.GetAttributes(profile) & FileAttributes.ReparsePoint) != 0)
                continue;
            var settingsFolder = Path.Combine(profile, EditorProfilePaths.SettingsFolder);
            var directory = Path.Combine(settingsFolder, "yedekler");
            if (Directory.Exists(directory) &&
                (File.GetAttributes(settingsFolder) & FileAttributes.ReparsePoint) == 0 &&
                (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0)
                yield return directory;
        }
    }

    internal string BackupSource(string path)
    {
        var full = LocalFilePaths.Resolve(path);
        var folder = Path.GetDirectoryName(full);
        if (!IsBackupName(Path.GetFileName(full)) ||
            !BackupFolders().Any(root => string.Equals(root, folder, StringComparison.OrdinalIgnoreCase)))
            throw new EditorFailure(EditorErrors.BackupPathOutsideScope);
        LocalFilePaths.RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length is <= 0 or > EditorLimits.DocumentBytes)
            throw new EditorFailure(EditorErrors.BackupInvalid);
        return full;
    }

    internal static bool IsBackupName(string name) =>
        name.Length > 5 && name.StartsWith('~') &&
        Path.GetExtension(name).Equals(FileExtensions.Udf, StringComparison.OrdinalIgnoreCase);

    private static string InRoot(string path, string root, string extension)
    {
        var full = LocalFilePaths.Resolve(path);
        var relative = Path.GetRelativePath(root, full);
        var name = Path.GetFileName(full);
        if (relative == "." || relative == ".." || relative.StartsWith(@"..\", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative) || !Path.GetExtension(name).Equals(extension, StringComparison.OrdinalIgnoreCase))
            throw new EditorFailure(EditorErrors.PathOutsideScope, extension);
        return full;
    }
}
