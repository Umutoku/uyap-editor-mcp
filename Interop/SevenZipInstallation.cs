using Microsoft.Win32;

namespace UyapEditorMcp;

internal static class SevenZipInstallation
{
    private const string WindowsInstallationKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion";
    private const string ProgramFilesValue = "ProgramFilesDir";
    private const string SevenZipDirectory = "7-Zip";
    private const string SevenZipExecutable = "7z.exe";

    internal static string ResolveExecutable()
    {
        // Use the machine installation directory without expanding process environment variables.
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var windows = machine.OpenSubKey(WindowsInstallationKey);
        if (windows?.GetValue(ProgramFilesValue, null, RegistryValueOptions.DoNotExpandEnvironmentNames)
            is not string programFiles || string.IsNullOrWhiteSpace(programFiles))
            throw new EditorException(EditorErrors.SevenZipMissing);

        var executable = Path.Combine(LocalFilePaths.Resolve(programFiles), SevenZipDirectory, SevenZipExecutable);
        if (!File.Exists(executable))
            throw new EditorException(EditorErrors.SevenZipMissing);
        LocalFilePaths.RequireExistingPlainPath(executable);
        return executable;
    }
}
