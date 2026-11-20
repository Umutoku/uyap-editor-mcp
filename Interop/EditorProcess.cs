using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace UyapEditorMcp;

internal sealed class EditorProcess : IDisposable
{
    private const string AssistiveTechnology =
        "-Djavax.accessibility.assistive_technologies=com.sun.java.accessibility.AccessBridge";
    private readonly Process process;
    private int sequence;
    private bool uncertain;

    internal EditorProcess(EditorSettings config, string profile, string stagedFile, bool createBlank, bool templateMode = false)
    {
        var start = new ProcessStartInfo(config.JavaExe)
        {
            WorkingDirectory = profile,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var argument in new[] { "-Duser.home=" + profile, AssistiveTechnology,
                     "-cp", config.ClassPath, "EditorLauncher", stagedFile })
            start.ArgumentList.Add(argument);
        if (templateMode)
            start.ArgumentList.Add(createBlank ? "template-new" : "template");
        else if (createBlank)
            start.ArgumentList.Add("new");

        start.Environment.Remove("JAVA_TOOL_OPTIONS");
        start.Environment.Remove("_JAVA_OPTIONS");
        start.Environment.Remove("JDK_JAVA_OPTIONS");
        process = Process.Start(start)
            ?? throw new EditorException(EditorErrors.EditorStartFailed);
        process.BeginErrorReadLine();
    }

    internal int Pid => process.Id;
    internal bool Exited => process.HasExited;
    internal bool IsUncertain => uncertain;

    internal async Task<string[]> Call(string action, CancellationToken cancellationToken, params string[] args)
    {
        if (uncertain)
            throw new EditorException(EditorErrors.EditorStateUncertain);
        if (process.HasExited)
            throw new EditorException(EditorErrors.EditorWindowClosed);

        cancellationToken.ThrowIfCancellationRequested();
        var id = (++sequence).ToString(CultureInfo.InvariantCulture);
        var request = string.Join('\t', new[] { id, action }.Concat(args.Select(Encode)));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(40));

        try
        {
            await process.StandardInput.WriteLineAsync(request.AsMemory(), timeout.Token);
            await process.StandardInput.FlushAsync(timeout.Token);
            var response = await process.StandardOutput.ReadLineAsync(timeout.Token);
            if (response == null && action == EditorBridgeCommands.Close)
                return ["closed"];
            var fields = response?.Split('\t');
            if (fields is not { Length: >= 2 } || fields[0] != id)
                throw new EditorException(EditorErrors.EditorProtocolError);
            var values = fields.Skip(2).Select(Decode).ToArray();
            if (fields[1] == "OK")
                return values;
            throw EditorException.FromNative(values.FirstOrDefault());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            uncertain = true;
            throw new EditorException(EditorErrors.EditorTimeout);
        }
        catch (OperationCanceledException)
        {
            uncertain = true;
            throw;
        }
        catch (IOException)
        {
            uncertain = true;
            throw new EditorException(EditorErrors.EditorConnectionLost);
        }
    }

    internal void AbortStartup()
    {
        if (!process.HasExited)
            process.Kill();
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
    public void Dispose()
    {
        process.StandardInput.Dispose();
        process.Dispose();
    }
}
