using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace XiaoXiIme.SandboxRunner;

internal static class NotepadKeyboardSmoke
{
    internal static async Task<int> RunAsync(string output)
    {
        var evidence = Path.Combine(output, "notepad.txt");
        var document = Path.Combine(output, $"note-{Guid.NewGuid():N}.txt");
        var reopenedDocument = Path.ChangeExtension(document, ".reopened.txt");
        File.WriteAllText(document, string.Empty);
        File.WriteAllText(reopenedDocument, string.Empty);
        using var layouts = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts")!;
        var id = layouts.GetSubKeyNames().Single(name =>
        {
            using var key = layouts.OpenSubKey(name);
            return string.Equals(key?.GetValue("Ime File") as string, "XiaoXiIme.ime", StringComparison.OrdinalIgnoreCase);
        });
        var layout = LoadKeyboardLayout(id, 0);
        if (layout == 0)
        {
            throw new InvalidOperationException($"Could not load layout {id}.");
        }

        try
        {
            var first = await OpenAsync(document);
            Record($"Initial window=0x{first:X}; initial HKL=0x{ActiveLayout(first):X}; XiaoXi HKL=0x{layout:X}");
            if (ActiveLayout(first) != layout)
            {
                var gui = FocusInfo(first);
                if (!PostMessage(gui.Focus != 0 ? gui.Focus : first, 0x0050, 0, layout))
                {
                    throw new InvalidOperationException("Could not request XiaoXi layout on the initial window.");
                }
                await WaitAsync(() => ActiveLayout(first) == layout, "initial layout selection");
            }
            var initial = await CheckAsync(first, document, "Initial");
            if (!PostMessage(first, 0x0010, 0, 0))
            {
                throw new InvalidOperationException("Could not close the initial Notepad window.");
            }
            await WaitAsync(() => !IsWindow(first), "initial Notepad close");
            var reopened = await OpenAsync(reopenedDocument);
            var second = await CheckAsync(reopened, reopenedDocument, "Reopened (no layout request)");
            PostMessage(reopened, 0x0010, 0, 0);
            return initial && second ? 0 : 1;
        }
        catch (Exception error) when (error is InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            Record(error.ToString());
            return 2;
        }

        async Task<nint> OpenAsync(string path)
        {
            using var launched = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "notepad.exe"))
            {
                UseShellExecute = false,
                ArgumentList = { path },
            });
            nint window = 0;
            await WaitAsync(() =>
            {
                window = GetForegroundWindow();
                GetWindowThreadProcessId(window, out var pid);
                if (pid == 0)
                {
                    return false;
                }
                var title = new System.Text.StringBuilder(512);
                GetWindowText(window, title, title.Capacity);
                return title.ToString().Contains(Path.GetFileNameWithoutExtension(path), StringComparison.OrdinalIgnoreCase);
            }, $"foreground Notepad for {Path.GetFileName(path)}");
            await Task.Delay(350);
            return window;
        }

        async Task<bool> CheckAsync(nint window, string path, string stage)
        {
            var gui = FocusInfo(window);
            Record($"{stage}: Window=0x{window:X}; Focus=0x{gui.Focus:X}; HKL=0x{ActiveLayout(window):X}");
            RequireForeground(window);
            Send((0x4B, false), (0x4B, true), (0x41, false), (0x41, true), (0x49, false), (0x49, true));
            var candidateShown = false;
            for (var i = 0; i < 15 && !candidateShown; i++)
            {
                await Task.Delay(100);
                var candidate = FindWindow(null, "XiaoXiIme Candidate Window");
                candidateShown = candidate != 0 && IsWindowVisible(candidate);
            }
            Record($"{stage}: CandidateVisible={candidateShown}");
            var statusPath = Path.Combine(output, "notepad-candidate-status.txt");
            if (File.Exists(statusPath))
            {
                using var stream = new FileStream(statusPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                Record($"{stage}: CandidateStatus={reader.ReadToEnd()}");
            }
            RequireForeground(window);
            Send((0x31, false), (0x31, true));
            await Task.Delay(700);
            RequireForeground(window);
            Send((0x11, false), (0x53, false), (0x53, true), (0x11, true));
            await Task.Delay(650);
            var text = File.ReadAllText(path);
            Record($"{stage}: Text='{text}'; final HKL=0x{ActiveLayout(window):X}");
            return candidateShown && text.Any(character => character is >= '\u4e00' and <= '\u9fff');
        }

        void Record(string text) => File.AppendAllText(evidence, text + Environment.NewLine);
    }

    private static async Task WaitAsync(Func<bool> condition, string stage)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            if (condition())
            {
                return;
            }
            await Task.Delay(100);
        } while (timer.Elapsed < TimeSpan.FromSeconds(12));
        throw new TimeoutException($"Timed out waiting for {stage}.");
    }

    private static XiaoXiIme.ImeInterop.GuiThreadInfo FocusInfo(nint window)
    {
        var info = new XiaoXiIme.ImeInterop.GuiThreadInfo { Size = (uint)Marshal.SizeOf<XiaoXiIme.ImeInterop.GuiThreadInfo>() };
        XiaoXiIme.ImeInterop.InputTargetMethods.GetGUIThreadInfo(GetWindowThreadProcessId(window, out _), ref info);
        return info;
    }

    private static nint ActiveLayout(nint window) => GetKeyboardLayout(GetWindowThreadProcessId(window, out _));

    private static void RequireForeground(nint window)
    {
        if (GetForegroundWindow() != window)
        {
            throw new InvalidOperationException("Notepad lost foreground; stopped sending keyboard input.");
        }
    }

    private static void Send(params (ushort Key, bool Up)[] keys)
    {
        var input = keys.Select(key => new Input { Type = 1, Data = new InputData { Key = key.Key, Flags = key.Up ? 2u : 0u } }).ToArray();
        if (SendInput((uint)input.Length, input, Marshal.SizeOf<Input>()) != input.Length)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        internal uint Type;
        internal InputData Data;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct InputData
    {
        [FieldOffset(0)] internal ushort Key;
        [FieldOffset(4)] internal uint Flags;
        [FieldOffset(24)] private nint _alignment;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] input, int size);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint thread);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint window, System.Text.StringBuilder text, int maxCount);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint window);
}
