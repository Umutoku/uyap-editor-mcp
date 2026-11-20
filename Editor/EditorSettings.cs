using System.Security.Cryptography;

namespace UyapEditorMcp;

internal sealed class EditorSettings
{
    internal const string Version = "5.4.20";

    private static readonly Dictionary<string, string> Package = new(StringComparer.OrdinalIgnoreCase)
    {
        ["editor_laf.jar"] = "32FF499305C9FAB7B35A4FAECDE88D95482863213A9F0873287A1E8D41FB48DD",
        ["editor_lib.jar"] = "00D7B060FE1BC2B5E3421244B97DD52F2473E6E91225115EF399E92B32EF5A95",
        ["editor_lib2.jar"] = "B958E996175273558201571EFB61E81C511E0A58AD4302CB80ACEFA73281100C",
        ["editor_utility.jar"] = "253E281F5A350F5A38ACD3E638FB7FA55E5E53B19438583F18E726F424420BAB",
        ["jai_hvl.jar"] = "014D7E1F7353F33688CB773A0E10FE305168DFE26DB5EBBEFB3520C0B7013D50",
        ["jdom.jar"] = "1BB7663B2E7F82DF2C864FF1D4748D419C8FCF8DA0CB0216D17A7BC18146F767",
        ["updater.jar"] = "2320A651193455BEA0B65626BF0457649B369845566E0B09E8905881DC13C38C",
    };

    internal EditorSettings(string editorDir, string javaBin, string inputRoot, string outputRoot, string stateDir,
        string? backupRoot = null)
    {
        EditorDir = DirectoryPath(editorDir);
        JavaBin = DirectoryPath(javaBin);
        InputRoot = DirectoryPath(inputRoot);
        OutputRoot = DirectoryPath(outputRoot);
        StateDir = DirectoryPath(stateDir);
        BackupRoot = backupRoot == null ? null : DirectoryPath(backupRoot);
        Classes = Path.Combine(AppContext.BaseDirectory, "java", "classes");
        if (!File.Exists(Path.Combine(Classes, "EditorLauncher.class")))
            throw new EditorFailure(EditorErrors.LauncherMissing);
        if (!File.Exists(JavaExe) || !File.Exists(BridgeDll))
            throw new EditorFailure(EditorErrors.JavaMissing);

        foreach (var (name, hash) in Package)
        {
            var path = Path.Combine(EditorDir, name);
            RequireExistingPlainPath(path);
            if (Sha256(path) != hash)
                throw new EditorFailure(EditorErrors.UnsupportedEditor, name, Version);
        }
    }

    internal string EditorDir { get; }
    internal string JavaBin { get; }
    internal string InputRoot { get; }
    internal string OutputRoot { get; }
    internal string StateDir { get; }
    internal string? BackupRoot { get; }
    internal string Classes { get; }
    internal string JavaExe => Path.Combine(JavaBin, "java.exe");
    internal string BridgeDll => Path.Combine(JavaBin, "WindowsAccessBridge-64.dll");
    internal string ClassPath => string.Join(Path.PathSeparator,
        new[] { Classes }.Concat(Package.Keys.Select(name => Path.Combine(EditorDir, name))));

    internal string Input(string path, bool allowOutputRoot = false)
        => ScopedInput(path, ".udf", 50L * 1024 * 1024, allowOutputRoot);

    internal string ArchiveInput(string path) => ScopedInput(path, ".7z", 51L * 1024 * 1024, true);

    internal string ArchivePasswordFile(string path) => ScopedInput(path, ".txt", 1024, false);

    private string ScopedInput(string path, string extension, long maxBytes, bool allowOutputRoot)
    {
        string full;
        try { full = InRoot(path, InputRoot, extension); }
        catch (EditorFailure failure) when (allowOutputRoot && failure.Code == EditorErrorCodes.PathOutsideScope)
        {
            full = InRoot(path, OutputRoot, extension);
        }
        RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length > maxBytes)
            throw new EditorFailure(EditorErrors.InputInvalid, extension, maxBytes);
        return full;
    }

    internal string ImportDocument(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".xml" or ".rtf" or ".usf"))
            throw new EditorFailure(EditorErrors.ImportFormatUnsupported);
        var full = InRoot(path, InputRoot, extension);
        RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length > 50 * 1024 * 1024)
            throw new EditorFailure(EditorErrors.ImportFileInvalid);
        return full;
    }

    internal string Image(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tif" or ".tiff"))
            throw new EditorFailure(EditorErrors.ImageFormatUnsupported);
        var full = InRoot(path, InputRoot, extension);
        RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length > 15 * 1024 * 1024)
            throw new EditorFailure(EditorErrors.ImageInvalid);
        return full;
    }

    internal string Output(string path, string extension)
    {
        var full = InRoot(path, OutputRoot, extension);
        RequireExistingPlainPath(Path.GetDirectoryName(full)!);
        if (File.Exists(full) || Directory.Exists(full))
            throw new EditorFailure(EditorErrors.OutputExists);
        return full;
    }

    internal IEnumerable<string> BackupFolders()
    {
        if (BackupRoot != null)
            yield return BackupRoot;
        foreach (var profile in Directory.EnumerateDirectories(StateDir))
        {
            if (!Guid.TryParseExact(Path.GetFileName(profile), "N", out _) ||
                (File.GetAttributes(profile) & FileAttributes.ReparsePoint) != 0)
                continue;
            var settingsFolder = Path.Combine(profile, ".uki");
            var directory = Path.Combine(settingsFolder, "yedekler");
            if (Directory.Exists(directory) &&
                (File.GetAttributes(settingsFolder) & FileAttributes.ReparsePoint) == 0 &&
                (File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0)
                yield return directory;
        }
    }

    internal string BackupSource(string path)
    {
        var full = LocalPath(path);
        var folder = Path.GetDirectoryName(full);
        if (!IsBackupName(Path.GetFileName(full)) ||
            !BackupFolders().Any(root => string.Equals(root, folder, StringComparison.OrdinalIgnoreCase)))
            throw new EditorFailure(EditorErrors.BackupPathOutsideScope);
        RequireExistingPlainPath(full);
        if (!File.Exists(full) || new FileInfo(full).Length is <= 0 or > 50 * 1024 * 1024)
            throw new EditorFailure(EditorErrors.BackupInvalid);
        return full;
    }

    internal static bool IsBackupName(string name) =>
        name.Length > 5 && name.StartsWith('~') &&
        Path.GetExtension(name).Equals(".udf", StringComparison.OrdinalIgnoreCase);

    internal static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string InRoot(string path, string root, string extension)
    {
        var full = LocalPath(path);
        var relative = Path.GetRelativePath(root, full);
        var name = Path.GetFileName(full);
        if (relative == "." || relative == ".." || relative.StartsWith(@"..\", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative) || !Path.GetExtension(name).Equals(extension, StringComparison.OrdinalIgnoreCase))
            throw new EditorFailure(EditorErrors.PathOutsideScope, extension);
        return full;
    }

    private static string DirectoryPath(string path)
    {
        var full = LocalPath(path);
        RequireExistingPlainPath(full);
        if (!Directory.Exists(full))
            throw new EditorFailure(EditorErrors.DirectoryMissing);
        return full;
    }

    private static string LocalPath(string path)
    {
        if (path.Length < 3 || !char.IsAsciiLetter(path[0]) || path[1] != ':' ||
            (path[2] != '\\' && path[2] != '/') || path.AsSpan(2).Contains(':') || path.Contains('\0'))
            throw new EditorFailure(EditorErrors.PathNotLocal);

        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (new DriveInfo(Path.GetPathRoot(full)!).DriveType != DriveType.Fixed)
            throw new EditorFailure(EditorErrors.PathNotOnFixedDrive);
        return full;
    }

    internal static void RequireExistingPlainPath(string path)
    {
        for (string? segment = path; segment != null; segment = Path.GetDirectoryName(segment))
        {
            if (!Path.Exists(segment))
                throw new EditorFailure(EditorErrors.PathMissing);
            if ((File.GetAttributes(segment) & FileAttributes.ReparsePoint) != 0)
                throw new EditorFailure(EditorErrors.PathRedirected);
        }
    }
}
