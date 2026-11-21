using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Switch;

internal sealed class MainForm : Form
{
    private readonly NotifyIcon _tray;
    private readonly KeyboardMonitor _monitor;
    private readonly Label _status;
    private readonly CheckBox _autoStart;
    private readonly string _dataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Switch");

    public MainForm()
    {
        Text = "Switch";
        Width = 430;
        Height = 220;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        Directory.CreateDirectory(_dataDirectory);
        CopyDefaultDictionary("ru.txt", "# Добавляйте по одному русскому слову на строку.\n");
        CopyDefaultDictionary("en.txt", "# Add one English word per line.\n");

        _status = new Label { Left = 20, Top = 20, Width = 370, Height = 42, Text = "Автопереключение включено." };
        _autoStart = new CheckBox { Left = 20, Top = 75, Width = 370, Text = "Запускать вместе с Windows", Checked = StartupManager.IsEnabled() };
        _autoStart.CheckedChanged += (_, _) => StartupManager.SetEnabled(_autoStart.Checked);
        var folder = new Button { Left = 20, Top = 112, Width = 180, Text = "Открыть словари" };
        folder.Click += (_, _) => Process.Start("explorer.exe", _dataDirectory);
        var hide = new Button { Left = 215, Top = 112, Width = 175, Text = "Свернуть в трей" };
        hide.Click += (_, _) => HideToTray();
        Controls.AddRange([_status, _autoStart, folder, hide]);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Открыть настройки", null, (_, _) => ShowWindow());
        menu.Items.Add("Включить / выключить", null, (_, _) => ToggleCorrection());
        menu.Items.Add("Выход", null, (_, _) => ExitApplication());
        _tray = new NotifyIcon { Text = "Switch", Icon = SystemIcons.Application, ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => ShowWindow();

        _monitor = new KeyboardMonitor(_dataDirectory, message => BeginInvoke(new Action(() => _status.Text = message)));
        _monitor.Start();
        FormClosing += (_, e) => { if (!_exiting) { e.Cancel = true; HideToTray(); } };
    }

    private bool _exiting;
    private void CopyDefaultDictionary(string name, string content)
    {
        var path = Path.Combine(_dataDirectory, name);
        if (!File.Exists(path)) File.WriteAllText(path, content, new UTF8Encoding(false));
    }
    private void HideToTray() { Hide(); ShowInTaskbar = false; }
    private void ShowWindow() { Show(); ShowInTaskbar = true; WindowState = FormWindowState.Normal; Activate(); }
    private void ToggleCorrection() { _monitor.Enabled = !_monitor.Enabled; _status.Text = _monitor.Enabled ? "Автопереключение включено." : "Автопереключение выключено."; }
    private void ExitApplication() { _exiting = true; _monitor.Dispose(); _tray.Visible = false; _tray.Dispose(); Application.Exit(); }
}

internal sealed class KeyboardMonitor : IDisposable
{
    private const int WhKeyboardLl = 13, WmKeyDown = 0x0100, WmSysKeyDown = 0x0104, WmKeyUp = 0x0101;
    private const uint KeyeventfKeyup = 0x0002, KeyeventfUnicode = 0x0004;
    private readonly WordCorrector _corrector;
    private readonly Action<string> _status;
    private readonly StringBuilder _word = new();
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hook;
    private bool _shift;
    private bool _injecting;
    private bool _pauseDown;
    private IntPtr _lastForegroundWindow;
    public bool Enabled { get; set; } = true;

    public KeyboardMonitor(string dataDirectory, Action<string> status)
    {
        _corrector = new WordCorrector(dataDirectory);
        _status = status;
        _proc = HookCallback;
    }
    public void Start()
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hook = SetWindowsHookEx(WhKeyboardLl, _proc, GetModuleHandle(module.ModuleName!), 0);
        if (_hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && !_injecting)
        {
            int message = wParam.ToInt32();
            var data = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            int vk = (int)data.VkCode;
            if (message == WmKeyDown || message == WmSysKeyDown)
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground != _lastForegroundWindow)
                {
                    _lastForegroundWindow = foreground;
                    _word.Clear();
                }

                if (vk == 0x13)
                {
                    if (!_pauseDown)
                    {
                        _pauseDown = true;
                        _status(LayoutSwitcher.ToggleForForegroundWindow()
                            ? "Раскладка переключена."
                            : "Не удалось переключить раскладку. Проверьте установленные языки Windows.");
                    }
                    return new IntPtr(1);
                }
                if (vk == 0x10 || vk == 0xA0 || vk == 0xA1) _shift = true;
                else if (vk == 0x08) { if (_word.Length > 0) _word.Length--; }
                else if (IsBoundary(vk))
                {
                    if (Enabled) CorrectCurrentWord();
                    _word.Clear();
                }
                else if (Enabled)
                {
                    char c = GetTypedCharacter(vk, data.ScanCode);
                    if (char.IsLetter(c) || c is '\'' or '-') _word.Append(c);
                    else if (_word.Length > 0 && vk != 0x11 && vk != 0x12) _word.Clear();
                }
            }
            else if (message == WmKeyUp)
            {
                if (vk == 0x13) { _pauseDown = false; return new IntPtr(1); }
                if (vk == 0x10 || vk == 0xA0 || vk == 0xA1) _shift = false;
            }
        }
        return CallNextHookEx(_hook, code, wParam, lParam);
    }

    private static bool IsBoundary(int vk) => vk is 0x20 or 0x0D or 0x09 or 0xBE or 0xBC or 0xBF or 0xBA or 0x6E;
    private char GetTypedCharacter(int vk, uint scanCode)
    {
        var state = new byte[256];
        if (!GetKeyboardState(state)) return '\0';
        state[0x10] = (byte)(_shift ? 0x80 : 0);

        IntPtr foreground = GetForegroundWindow();
        uint threadId = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out _);
        IntPtr layout = threadId == 0 ? IntPtr.Zero : GetKeyboardLayout(threadId);
        if (layout == IntPtr.Zero) return '\0';

        var buffer = new StringBuilder(8);
        int count = ToUnicodeEx((uint)vk, scanCode, state, buffer, buffer.Capacity, 0, layout);
        return count > 0 && buffer.Length > 0 ? buffer[0] : '\0';
    }

    private void CorrectCurrentWord()
    {
        string original = _word.ToString();
        string? replacement = _corrector.TryCorrect(original);
        if (replacement is null || replacement == original) return;
        _injecting = true;
        try
        {
            var inputs = new List<Input>();
            for (int i = 0; i < original.Length; i++)
            {
                inputs.Add(KeyInput(0x08, false));
                inputs.Add(KeyInput(0x08, true));
            }
            foreach (char c in replacement)
            {
                inputs.Add(UnicodeInput(c, false));
                inputs.Add(UnicodeInput(c, true));
            }
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
            _status($"Исправлено: {original} → {replacement}");
        }
        finally { _injecting = false; }
    }

    private static Input KeyInput(ushort key, bool keyUp) => new() { Type = 1, U = new InputUnion { Ki = new KeyboardInput { WVk = key, DwFlags = keyUp ? KeyeventfKeyup : 0 } } };
    private static Input UnicodeInput(char c, bool keyUp) => new() { Type = 1, U = new InputUnion { Ki = new KeyboardInput { WScan = c, DwFlags = KeyeventfUnicode | (keyUp ? KeyeventfKeyup : 0) } } };

    public void Dispose() { if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; } }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct KbdLlHookStruct { public uint VkCode, ScanCode, Flags, Time; public UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort WVk, WScan; public uint DwFlags, Time; public UIntPtr DwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct InputUnion { public KeyboardInput Ki; }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion U; }

    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hMod, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll")] private static extern bool GetKeyboardState(byte[] state);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int ToUnicodeEx(uint vk, uint scan, byte[] state, [Out] StringBuilder buffer, int size, uint flags, IntPtr layout);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
}

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Switch";
    public static bool IsEnabled()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) is string;
    }
    public static void SetEnabled(bool enabled)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(AppName, $"\\\"{Application.ExecutablePath}\\\"");
        else key.DeleteValue(AppName, false);
    }
}
