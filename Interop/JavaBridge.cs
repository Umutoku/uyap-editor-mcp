using System.Runtime.InteropServices;

namespace UyapEditorMcp;

internal sealed unsafe class JavaBridge
{
    private const string LibraryName = "UyapJavaAccessBridge";
    private const int ShortString = 256;

    // The JAB client owns a hidden Win32 window on the calling thread.
    internal JavaBridge(string libraryPath)
    {
        var module = NativeLibrary.Load(libraryPath);
        NativeLibrary.SetDllImportResolver(typeof(JavaBridge).Assembly,
            (name, _, _) => name == LibraryName ? module : 0);
        Windows_run();
    }

    internal sealed record Control(string Name, string Role, string States)
    {
        internal bool State(string value) => States.Split(',').Contains(value, StringComparer.Ordinal);
    }

    internal sealed class Tree(int vm, long root) : IDisposable
    {
        private readonly List<long> references = [root];

        internal IEnumerable<Control> Controls()
        {
            var pending = new Stack<(long Id, int Depth)>();
            pending.Push((root, 0));
            var count = 0;
            while (pending.TryPop(out var entry))
            {
                if (++count > 1200)
                    yield break;
                if (!TryDescribe(vm, entry.Id, out var control, out var children))
                    continue;
                yield return control;

                // Rich document content has a separate text API and can contain thousands of children.
                if (entry.Depth >= 24 || control.Role == "text" || !control.State("visible"))
                    continue;
                for (var index = Math.Min(children, 1200 - count) - 1; index >= 0; index--)
                {
                    var child = getAccessibleChildFromContext(vm, entry.Id, index);
                    if (child == 0)
                        continue;
                    references.Add(child);
                    pending.Push((child, entry.Depth + 1));
                }
            }
        }

        public void Dispose()
        {
            for (var i = references.Count - 1; i >= 0; i--)
                releaseJavaObject(vm, references[i]);
            references.Clear();
        }
    }

    internal Tree? ReadWindow(nint hwnd) =>
        getAccessibleContextFromHWND(hwnd, out var vm, out var root) != 0 && root != 0 ? new Tree(vm, root) : null;

    private static bool TryDescribe(int vm, long id, out Control control, out int children)
    {
        var info = new ContextInfo();
        children = 0;
        if (getAccessibleContextInfo(vm, id, ref info) == 0)
        {
            control = null!;
            return false;
        }

        children = info.Children;
        control = new Control(Read(info.Name, 1024), Read(info.EnglishRole, ShortString),
            Read(info.EnglishStates, ShortString));
        return true;
    }

    private static string Read(char* text, int maxLength)
    {
        var span = new ReadOnlySpan<char>(text, maxLength);
        var end = span.IndexOf((char)0);
        return new string(end < 0 ? span : span[..end]);
    }

    // Java Access Bridge x64 uses Windows 32-bit long for VM IDs and jlong for object handles.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ContextInfo
    {
        internal fixed char Name[1024];
        private fixed char description[1024];
        private fixed char role[ShortString];
        internal fixed char EnglishRole[ShortString];
        private fixed char states[ShortString];
        internal fixed char EnglishStates[ShortString];
        private int index;
        internal int Children;
        private int x;
        private int y;
        private int width;
        private int height;
        private int component;
        private int action;
        private int selection;
        private int text;
        private int interfaces;
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)] private static extern void Windows_run();
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int getAccessibleContextFromHWND(nint hwnd, out int vm, out long root);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int getAccessibleContextInfo(int vm, long id, ref ContextInfo info);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern long getAccessibleChildFromContext(int vm, long id, int child);
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void releaseJavaObject(int vm, long id);
}
