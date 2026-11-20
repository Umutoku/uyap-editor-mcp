using System.Security.Cryptography;

namespace UyapEditorMcp;

internal static class LocalFilePaths
{
    internal static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static string ResolveDirectory(string path)
    {
        var full = Resolve(path);
        RequireExistingPlainPath(full);
        if (!Directory.Exists(full))
            throw new EditorException(EditorErrors.DirectoryMissing);
        return full;
    }

    internal static string Resolve(string path)
    {
        if (path.Length < 3 || !char.IsAsciiLetter(path[0]) || path[1] != ':' ||
            (path[2] != '\\' && path[2] != '/') || path.AsSpan(2).Contains(':') || path.Contains('\0'))
            throw new EditorException(EditorErrors.PathNotLocal);

        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (new DriveInfo(Path.GetPathRoot(full)!).DriveType != DriveType.Fixed)
            throw new EditorException(EditorErrors.PathNotOnFixedDrive);
        return full;
    }

    internal static void RequireExistingPlainPath(string path)
    {
        for (string? segment = path; segment != null; segment = Path.GetDirectoryName(segment))
        {
            if (!Path.Exists(segment))
                throw new EditorException(EditorErrors.PathMissing);
            if ((File.GetAttributes(segment) & FileAttributes.ReparsePoint) != 0)
                throw new EditorException(EditorErrors.PathRedirected);
        }
    }
}
