using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UyapEditorMcp;

internal sealed class BridgeReader : IDisposable
{
    private readonly BlockingCollection<Action<JavaBridge>> queue = new();
    private readonly Thread thread;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile BridgeState status = BridgeState.BridgeReady;
    internal BridgeState Status => status;
    private static readonly Regex PageLabel = new(@"^Sayfa\s*:\s*(\d+)\s*/\s*(\d+)\s*$",
        RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    internal BridgeReader(EditorSettings settings)
    {
        thread = new Thread(() => Run(settings.BridgeDll)) { IsBackground = true, Name = "Java Access Bridge" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    internal Task<PageResult?> Page(int pid, CancellationToken cancellationToken) =>
        Query(pid, tree =>
        {
            foreach (var node in tree.Controls().Where(control => control.Role == "label"))
            {
                var match = PageLabel.Match(node.Name);
                if (match.Success)
                    return new PageResult(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), "Java Access Bridge");
            }
            return null;
        }, cancellationToken);

    private async Task<T?> Query<T>(int pid, Func<JavaBridge.Tree, T?> read, CancellationToken cancellationToken)
        where T : class
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            await ready.Task.WaitAsync(timeout.Token);
            var result = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var queryToken = timeout.Token;
            queue.Add(bridge => CompleteQuery(bridge, pid, read, result, queryToken), queryToken);

            return await result.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            status = BridgeState.BridgeTimedOut;
            return null;
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            status = BridgeState.BridgeUnavailable;
            return null;
        }
    }

    private void CompleteQuery<T>(JavaBridge bridge, int pid, Func<JavaBridge.Tree, T?> read,
        TaskCompletionSource<T?> result, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var window = Desktop.Windows(pid).SingleOrDefault(candidate => candidate.Title.Contains(
                EditorWindowTitle.VersionMarker, StringComparison.Ordinal));
            if (window == null || cancellationToken.IsCancellationRequested)
            {
                status = window == null ? BridgeState.BridgeWindowMissing : BridgeState.BridgeTimedOut;
                result.TrySetResult(null);
                return;
            }

            using var tree = bridge.ReadWindow(window.Handle);
            var snapshot = tree == null ? null : read(tree);
            status = (tree, snapshot) switch
            {
                (null, _) => BridgeState.BridgeContextMissing,
                (_, null) => BridgeState.BridgeElementMissing,
                _ => BridgeState.BridgeConnected,
            };
            result.TrySetResult(snapshot);
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }
    }

    private void Run(string dll)
    {
        try
        {
            var bridge = new JavaBridge(dll);
            ready.TrySetResult();
            while (!queue.IsCompleted)
            {
                Desktop.Pump();
                if (queue.TryTake(out var next, 25))
                    next(bridge);
            }
        }
        catch (Exception error)
        {
            ready.TrySetException(error);
        }
    }

    public void Dispose()
    {
        queue.CompleteAdding();
        if (thread.Join(TimeSpan.FromSeconds(1)))
            queue.Dispose();
    }
}
