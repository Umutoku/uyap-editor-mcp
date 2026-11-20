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
        EditorDir = LocalFilePaths.ResolveDirectory(editorDir);
        JavaBin = LocalFilePaths.ResolveDirectory(javaBin);
        InputRoot = LocalFilePaths.ResolveDirectory(inputRoot);
        OutputRoot = LocalFilePaths.ResolveDirectory(outputRoot);
        StateDir = LocalFilePaths.ResolveDirectory(stateDir);
        BackupRoot = backupRoot == null ? null : LocalFilePaths.ResolveDirectory(backupRoot);
        Classes = Path.Combine(AppContext.BaseDirectory, "java", "classes");
        if (!File.Exists(Path.Combine(Classes, "EditorLauncher.class")))
            throw new EditorException(EditorErrors.LauncherMissing);
        if (!File.Exists(JavaExe) || !File.Exists(BridgeDll))
            throw new EditorException(EditorErrors.JavaMissing);

        foreach (var (name, hash) in Package)
        {
            var path = Path.Combine(EditorDir, name);
            LocalFilePaths.RequireExistingPlainPath(path);
            if (LocalFilePaths.Sha256(path) != hash)
                throw new EditorException(EditorErrors.UnsupportedEditor, name, Version);
        }
    }

    internal string EditorDir
    {
        get;
    }
    internal string JavaBin
    {
        get;
    }
    internal string InputRoot
    {
        get;
    }
    internal string OutputRoot
    {
        get;
    }
    internal string StateDir
    {
        get;
    }
    internal string? BackupRoot
    {
        get;
    }
    internal string Classes
    {
        get;
    }
    internal string JavaExe => Path.Combine(JavaBin, "java.exe");
    internal string BridgeDll => Path.Combine(JavaBin, "WindowsAccessBridge-64.dll");
    internal string ClassPath => string.Join(Path.PathSeparator,
        new[] { Classes }.Concat(Package.Keys.Select(name => Path.Combine(EditorDir, name))));
}
