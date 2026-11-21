using System.Runtime.InteropServices;

namespace Switch;

internal static class LayoutSwitcher
{
    private const uint WmInputLangChangeRequest = 0x0050;
    private const uint KlfActivate = 0x00000001;
    private const uint KlfSubstituteOk = 0x00000002;

    public static bool ToggleForForegroundWindow()
    {
        IntPtr window = GetForegroundWindow();
        if (window == IntPtr.Zero) return false;

        uint threadId = GetWindowThreadProcessId(window, out _);
        IntPtr current = GetKeyboardLayout(threadId);
        ushort language = unchecked((ushort)((long)current & 0xFFFF));

        string target = language == 0x0419 ? "00000409" : "00000419";
        IntPtr layout = LoadKeyboardLayout(target, KlfActivate | KlfSubstituteOk);
        if (layout == IntPtr.Zero) return false;

        return PostMessage(window, WmInputLangChangeRequest, IntPtr.Zero, layout);
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
