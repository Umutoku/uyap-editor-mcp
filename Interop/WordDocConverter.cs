using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;

namespace UyapEditorMcp;

internal static class WordDocConverter
{
    internal const string WorkerArgument = "--convert-word-doc";
    internal const string Command = "compat:Microsoft Word/DOC";
    private const int RuntimeUnavailable = 10;
    private const int SharedWordInstance = 11;
    private const int ConversionUnverified = 12;
    private const int UnsafeDocument = 13;
    private const int WordBinaryFormat = 0;
    private const int DoNotSaveChanges = 0;
    private const int DisableMacros = 3;
    private static readonly HashSet<string> ActiveRtfControls = new(
        ["field", "datafield", "object", "objdata", "objautlink", "objlink", "template", "filetbl", "file",
         "linkself", "linkval", "linkstyles", "htmltag", "mhtmltag", "datastore", "bin"],
        StringComparer.OrdinalIgnoreCase);

    internal static async Task Convert(string input, string output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var executable = Environment.ProcessPath
            ?? throw new EditorFailure(EditorErrors.DocWorkerMissing);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(input)!,
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
        start.ArgumentList.Add(WorkerArgument);
        start.ArgumentList.Add(input);
        start.ArgumentList.Add(output);
        using var worker = Process.Start(start)
            ?? throw new EditorFailure(EditorErrors.DocWorkerStartFailed);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            await worker.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try { worker.Kill(); }
            catch (InvalidOperationException) { }
            if (token.IsCancellationRequested)
                throw;
            throw new EditorFailure(EditorErrors.DocWordTimeout);
        }
        switch (worker.ExitCode)
        {
            case 0:
                return;
            case RuntimeUnavailable:
                throw new EditorFailure(EditorErrors.DocWordRuntimeUnavailable);
            case SharedWordInstance:
                throw new EditorFailure(EditorErrors.DocWordInstanceBusy);
            case UnsafeDocument:
                throw new EditorFailure(EditorErrors.DocWordDynamicContentUnsupported);
            default:
                throw new EditorFailure(EditorErrors.DocWordConversionUnverified);
        }
    }

    internal static int RunWorker(string input, string output)
    {
        int code = ConversionUnverified;
        var thread = new Thread(() => code = ConvertAndVerify(input, output));
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return code;
    }

    private static int ConvertAndVerify(string input, string output)
    {
        dynamic? word = null;
        dynamic? documents = null;
        bool ownsInstance = false;
        int? originalSecurity = null;
        try
        {
            if (!IsStage(input, ".rtf") || !IsStage(output, ".doc") || !File.Exists(input) ||
                Path.Exists(output) || !string.Equals(Path.GetDirectoryName(input), Path.GetDirectoryName(output),
                    StringComparison.OrdinalIgnoreCase))
                return ConversionUnverified;
            EditorSettings.RequireExistingPlainPath(input);
            if (HasActiveRtfControls(input))
                return UnsafeDocument;
            Type? wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null)
                return RuntimeUnavailable;
            word = Activator.CreateInstance(wordType);
            if (word == null)
                return RuntimeUnavailable;
            documents = word.Documents;
            if (word.Visible || word.UserControl || documents.Count != 0)
                return SharedWordInstance;
            ownsInstance = true;
            word.DisplayAlerts = 0;
            originalSecurity = (int)word.AutomationSecurity;
            word.AutomationSecurity = DisableMacros;

            string expected = ConvertOpenDocument(documents, input, output);
            dynamic reopened = OpenReadOnly(documents, output);
            try
            {
                return (int)reopened.SaveFormat == WordBinaryFormat && ReadText(reopened) == expected
                    ? 0 : ConversionUnverified;
            }
            finally
            {
                CloseDocument(reopened);
            }
        }
        catch (COMException e) when (word == null && e.HResult == unchecked((int)0x80040154))
        {
            return RuntimeUnavailable;
        }
        catch (Exception e) when (e is COMException or RuntimeBinderException or IOException or UnauthorizedAccessException or EditorFailure)
        {
            Console.Error.WriteLine($"DOC Word worker failed ({e.GetType().Name}, 0x{e.HResult:X8}).");
            return ConversionUnverified;
        }
        finally
        {
            if (ownsInstance && (object?)word is { } ownedWord)
            {
                if (originalSecurity is { } previousSecurity)
                    Restore(() => ((dynamic)ownedWord).AutomationSecurity = previousSecurity);
                Restore(() => ((dynamic)ownedWord).Quit(SaveChanges: DoNotSaveChanges));
            }
            Release(documents);
            Release(word);
        }
    }

    private static bool HasActiveRtfControls(string path)
    {
        using var input = new StreamReader(path, System.Text.Encoding.Latin1, detectEncodingFromByteOrderMarks: false);
        Span<char> control = stackalloc char[32];
        while (input.Read() is var character && character != -1)
        {
            if (character != '\\')
                continue;
            int next = input.Read();
            if (next == -1 || !char.IsAsciiLetter((char)next))
                continue;
            var length = 0;
            control[length++] = (char)next;
            while (input.Peek() is var pending && pending != -1 && char.IsAsciiLetter((char)pending))
            {
                if (length == control.Length)
                    return true;
                control[length++] = (char)input.Read();
            }
            if (ActiveRtfControls.Contains(new string(control[..length])))
                return true;
        }
        return false;
    }

    private static bool IsStage(string path, string extension)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return Path.IsPathFullyQualified(path) &&
            Path.GetPathRoot(path) is { Length: 3 } root && root[1] == ':' &&
            new DriveInfo(root).DriveType == DriveType.Fixed &&
            string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase) &&
            name.StartsWith(".uyap-", StringComparison.Ordinal) && Guid.TryParseExact(name.AsSpan(6), "N", out _);
    }

    private static string ConvertOpenDocument(dynamic documents, string input, string output)
    {
        dynamic document = OpenReadOnly(documents, input);
        try
        {
            string text = ReadText(document);
            document.SaveAs2(FileName: output, FileFormat: WordBinaryFormat, AddToRecentFiles: false);
            return text;
        }
        finally
        {
            CloseDocument(document);
        }
    }

    private static object OpenReadOnly(dynamic documents, string path) => documents.Open(FileName: path,
        ConfirmConversions: false, ReadOnly: true, AddToRecentFiles: false, Visible: false, NoEncodingDialog: true);

    private static string ReadText(dynamic document)
    {
        dynamic content = document.Content;
        try { return (string)content.Text; }
        finally { Release(content); }
    }

    private static void CloseDocument(dynamic document)
    {
        try { document.Close(SaveChanges: DoNotSaveChanges); }
        finally { Release(document); }
    }

    private static void Restore(Action operation)
    {
        try { operation(); }
        catch (COMException) { }
        catch (RuntimeBinderException) { }
    }

    private static void Release(object? value)
    {
        if (value != null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }
}
