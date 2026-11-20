using System.Runtime.InteropServices;
using System.Text;

namespace UyapEditorMcp;

internal static class Desktop
{
    private const uint RemoveMessage = 1;

    internal sealed record Window(nint Handle, string Title);

    internal static List<Window> Windows(int pid)
    {
        List<Window> found = [];
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var owner);
            if (owner == (uint)pid && IsWindowVisible(hwnd))
            {
                var title = new StringBuilder(2048);
                GetWindowText(hwnd, title, title.Capacity);
                if (title.Length > 0)
                    found.Add(new Window(hwnd, title.ToString().Trim()));
            }
            return true;
        }, 0);
        return found;
    }

    internal static void Pump()
    {
        while (PeekMessage(out var message, 0, 0, 0, RemoveMessage))
        {
            TranslateMessage(ref message);
            DispatchMessage(ref message);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Point(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        private nint window;
        private uint message;
        private nuint wParam;
        private nint lParam;
        private uint time;
        private Point point;
        private uint unused;
    }

    private delegate bool EnumCallback(nint window, nint argument);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, nint argument);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint window, StringBuilder text, int capacity);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    private static extern bool PeekMessage(out Message message, nint window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern nint DispatchMessage(ref Message message);
}
