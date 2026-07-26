using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeUi.Avalonia;

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("candidate-window-state", RunCandidateWindowStateAsync),
    ("ime-host-ipc", RunImeHostIpcAsync),
    ("real-ime-keystroke-commit", RunRealImeKeystrokeCommitAsync),
};

foreach (var scenario in scenarios)
{
    try
    {
        await scenario.Run();
        Console.WriteLine($"PASS {scenario.Name}");
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"FAIL {scenario.Name}: {exception}");
        return 1;
    }
}

return 0;

static Task RunCandidateWindowStateAsync()
{
    var controller = new CandidateWindowController();
    var candidates = Enumerable.Range(0, 12)
        .Select(index => new ImeCandidate($"候选{index}", $"read{index}"))
        .ToArray();
    var uiState = new ImeUiState(
        CandidateWindowVisible: true,
        new CompositionText("ni", "ni", 2),
        candidates,
        new ImeCandidateWindowState(10, 9, 3),
        new ImeGuideline(ImeGuidelineLevel.Reading, "ni"),
        AnchorX: 100,
        AnchorY: 200);

    var state = controller.Update(uiState);

    Ensure(state.IsVisible, "Candidate window should be visible.");
    Ensure(state.CompositionText == "ni", "Composition text mismatch.");
    Ensure(state.PageStart == 9 && state.PageSize == 3, "Candidate page mismatch.");
    Ensure(state.CurrentPage == 4 && state.TotalPages == 4, "Candidate page count mismatch.");
    Ensure(state.Candidates.Count == 3, "Candidate count mismatch.");
    Ensure(state.Selection == 10, "Candidate selection mismatch.");
    Ensure(state.Candidates.Single(candidate => candidate.IsSelected).DisplayIndex == 2, "Selected candidate display index mismatch.");
    Ensure(state.AnchorX == 100 && state.AnchorY == 200, "Candidate anchor mismatch.");

    return Task.CompletedTask;
}

static async Task RunImeHostIpcAsync()
{
    var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Integration_{Guid.NewGuid():N}");
    using var host = new ImeHostService(options);
    using var client = new XiaoXiImeIpcClient(options);

    host.Start();
    await client.ConnectAsync();

    var status = await client.GetHostStatusAsync();
    await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
    var composingResult = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));
    var uiState = await client.GetUiStateAsync();

    Ensure(status.IsRunning, "IME host should be running.");
    Ensure(composingResult.Handled && composingResult.Snapshot.IsComposing, "IME should be composing after 'ni'.");
    Ensure(composingResult.Snapshot.Composition.Reading == "ni", "Composition reading mismatch.");
    Ensure(composingResult.Snapshot.Candidates.Count > 0 && composingResult.Snapshot.Candidates[0].Text == "你", "Expected candidate was not returned.");
    Ensure(uiState.CandidateWindowVisible, "Candidate window state should be visible.");

    var commitResult = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));
    Ensure(commitResult.Handled && commitResult.CommitText == "你", "Candidate commit mismatch.");
    Ensure(!commitResult.Snapshot.IsComposing, "Composition should end after commit.");
}

static Task RunRealImeKeystrokeCommitAsync()
{
    if (!OperatingSystem.IsWindows())
    {
        throw new PlatformNotSupportedException("The real IME keystroke scenario requires Windows.");
    }

    return Win32ImeEditScenario.RunAsync();
}

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

[SupportedOSPlatform("windows")]
internal static class Win32ImeEditScenario
{
    private const string KeyboardLayoutsRegistryPath = @"SYSTEM\CurrentControlSet\Control\Keyboard Layouts";
    private const string ExpectedLayoutText = "XiaoXi IME";
    private const string ExpectedImeFile = "XiaoXiIme.ime";
    private const uint KlFActivate = 0x00000001;
    private const uint WsOverlappedWindow = 0x00CF0000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsBorder = 0x00800000;
    private const uint EsAutoHScroll = 0x0080;
    private const int SwShow = 5;
    private const uint PmRemove = 0x0001;
    private const uint InputKeyboard = 1;
    private const uint KeyEventFKeyUp = 0x0002;
    private const ushort VkX = 0x58;

    public static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The real IME keystroke scenario requires Windows.");
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => RunOnWindowThread(completion))
        {
            IsBackground = true,
            Name = "XiaoXiIme real keystroke integration",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    private static void RunOnWindowThread(TaskCompletionSource completion)
    {
        nint window = 0;
        nint keyboardLayout = 0;
        try
        {
            var layoutId = FindInstalledLayoutId();
            keyboardLayout = LoadKeyboardLayout(layoutId, KlFActivate);
            if (keyboardLayout == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to load XiaoXi IME keyboard layout {layoutId}.");
            }

            window = CreateWindowEx(
                0,
                "STATIC",
                "XiaoXiIme Integration Test",
                WsOverlappedWindow | WsVisible,
                100,
                100,
                480,
                160,
                0,
                0,
                GetModuleHandle(null),
                0);
            if (window == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to create the integration test window.");
            }

            var edit = CreateWindowEx(
                0,
                "EDIT",
                string.Empty,
                WsChild | WsVisible | WsBorder | EsAutoHScroll,
                20,
                30,
                420,
                32,
                window,
                0,
                GetModuleHandle(null),
                0);
            if (edit == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to create the integration test EDIT control.");
            }

            ShowWindow(window, SwShow);
            UpdateWindow(window);
            ActivateKeyboardLayout(keyboardLayout, 0);
            SetForegroundWindow(window);
            SetActiveWindow(window);
            SetFocus(edit);
            PumpMessages();

            if (GetForegroundWindow() != window || GetFocus() != edit)
            {
                throw new InvalidOperationException("The integration test could not acquire the foreground window and EDIT focus required by SendInput.");
            }

            SendXx();

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            string text;
            do
            {
                PumpMessages();
                text = GetWindowText(edit);
                if (text == "小希")
                {
                    completion.SetResult();
                    return;
                }

                Thread.Sleep(10);
            }
            while (DateTime.UtcNow < deadline);

            throw new InvalidOperationException($"Expected the EDIT control text to be '小希' after typing 'xx', but it was '{text}'.");
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            if (window != 0)
            {
                DestroyWindow(window);
            }

            if (keyboardLayout != 0)
            {
                UnloadKeyboardLayout(keyboardLayout);
            }
        }
    }

    private static string FindInstalledLayoutId()
    {
        using var layouts = Registry.LocalMachine.OpenSubKey(KeyboardLayoutsRegistryPath);
        if (layouts is null)
        {
            throw new InvalidOperationException($"Unable to open HKLM\\{KeyboardLayoutsRegistryPath}.");
        }

        foreach (var layoutId in layouts.GetSubKeyNames())
        {
            using var layout = layouts.OpenSubKey(layoutId);
            var layoutText = layout?.GetValue("Layout Text") as string;
            var imeFile = layout?.GetValue("Ime File") as string;
            if (string.Equals(layoutText, ExpectedLayoutText, StringComparison.OrdinalIgnoreCase)
                && string.Equals(imeFile, ExpectedImeFile, StringComparison.OrdinalIgnoreCase))
            {
                return layoutId;
            }
        }

        throw new InvalidOperationException("The installed XiaoXi IME keyboard layout was not found in HKLM.");
    }

    private static void SendXx()
    {
        var inputSize = Marshal.SizeOf<Input>();
        var inputUnionOffset = Marshal.OffsetOf<Input>(nameof(Input.Union)).ToInt32();
        if (inputSize != 40 || inputUnionOffset != 8)
        {
            throw new InvalidOperationException(
                $"Unexpected Win32 INPUT layout for {RuntimeInformation.ProcessArchitecture}: size={inputSize}, unionOffset={inputUnionOffset}; expected size=40, unionOffset=8.");
        }

        var inputs = new[]
        {
            CreateKeyboardInput(VkX, 0),
            CreateKeyboardInput(VkX, KeyEventFKeyUp),
            CreateKeyboardInput(VkX, 0),
            CreateKeyboardInput(VkX, KeyEventFKeyUp),
        };

        var sent = SendInput((uint)inputs.Length, inputs, inputSize);
        if (sent != inputs.Length)
        {
            throw new Win32Exception(
                Marshal.GetLastPInvokeError(),
                $"SendInput injected {sent} of {inputs.Length} keyboard events. ProcessArchitecture={RuntimeInformation.ProcessArchitecture}, InputSize={inputSize}, InputUnionOffset={inputUnionOffset}, ForegroundWindow=0x{GetForegroundWindow():X}, FocusWindow=0x{GetFocus():X}.");
        }
    }

    private static Input CreateKeyboardInput(ushort virtualKey, uint flags)
    {
        return new Input
        {
            Type = InputKeyboard,
            Union = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = virtualKey,
                    Flags = flags,
                },
            },
        };
    }

    private static void PumpMessages()
    {
        while (PeekMessage(out var message, 0, 0, 0, PmRemove))
        {
            TranslateMessage(message);
            DispatchMessage(message);
        }
    }

    private static string GetWindowText(nint window)
    {
        var length = GetWindowTextLength(window);
        var buffer = new StringBuilder(length + 1);
        GetWindowText(window, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Input
    {
        [FieldOffset(0)]
        public uint Type;

        [FieldOffset(8)]
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Window;
        public uint Value;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public Point Point;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", EntryPoint = "LoadKeyboardLayoutW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadKeyboardLayout(string keyboardLayoutId, uint flags);

    [DllImport("user32.dll")]
    private static extern nint ActivateKeyboardLayout(nint keyboardLayout, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnloadKeyboardLayout(nint keyboardLayout);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(uint extendedStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint SetActiveWindow(nint window);

    [DllImport("user32.dll")]
    private static extern nint SetFocus(nint window);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out Message message, nint window, uint minimumMessage, uint maximumMessage, uint removeMessage);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(in Message message);

    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern nint DispatchMessage(in Message message);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW")]
    private static extern int GetWindowTextLength(nint window);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint window, StringBuilder text, int maximumCount);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);
}
