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
    private const uint LoadLibrarySearchSystem32 = 0x00000800;
    private const string ResetDiagnosticsExport = "XiaoXiImeResetKeystrokeDiagnostics";
    private const string GetDiagnosticsExport = "XiaoXiImeGetKeystrokeDiagnostics";

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

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(90));
    }

    private static void RunOnWindowThread(TaskCompletionSource completion)
    {
        nint window = 0;
        nint keyboardLayout = 0;
        nint imeModule = 0;
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
                "XiaoXiIme Integration Test - 请用键盘输入 xx",
                WsOverlappedWindow | WsVisible,
                100,
                100,
                520,
                190,
                0,
                0,
                GetModuleHandle(null),
                0);
            if (window == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to create the integration test window.");
            }

            var prompt = CreateWindowEx(
                0,
                "STATIC",
                "请在下面的输入框中用键盘输入 xx（不要粘贴）",
                WsChild | WsVisible,
                20,
                20,
                470,
                24,
                window,
                0,
                GetModuleHandle(null),
                0);
            if (prompt == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to create the manual input prompt.");
            }

            var edit = CreateWindowEx(
                0,
                "EDIT",
                string.Empty,
                WsChild | WsVisible | WsBorder | EsAutoHScroll,
                20,
                55,
                470,
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
            SetForegroundWindow(window);
            SetActiveWindow(window);
            SetFocus(edit);
            ActivateAndOpenIme(edit, keyboardLayout);
            PumpMessages();

            if (GetForegroundWindow() != window || GetFocus() != edit)
            {
                throw new InvalidOperationException("The integration test could not acquire the foreground window and EDIT focus required for manual keyboard input.");
            }

            imeModule = LoadLibraryEx(ExpectedImeFile, 0, LoadLibrarySearchSystem32);
            if (imeModule == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to load {ExpectedImeFile} from System32 for keystroke diagnostics.");
            }

            var diagnostics = ImeDiagnosticsExports.Load(imeModule);
            diagnostics.Reset();
            Console.WriteLine("ACTION real-ime-keystroke-commit: 请在测试窗口中用键盘输入 xx（不要粘贴）。");

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
            string text;
            do
            {
                PumpMessages();
                text = GetWindowText(edit);
                if (text == "小希")
                {
                    var snapshot = diagnostics.GetSnapshot();
                    if (snapshot.ImeProcessKeyCallCount < 2 || snapshot.ImeToAsciiExCallCount < 2)
                    {
                        throw new InvalidOperationException($"The text committed, but the IME call trace was incomplete. {snapshot}");
                    }

                    completion.SetResult();
                    return;
                }

                Thread.Sleep(10);
            }
            while (DateTime.UtcNow < deadline);

            throw new InvalidOperationException(
                $"Timed out waiting for the user to type 'xx'. Expected the EDIT control text to be '小希', but it was '{text}'. {GetImeState(edit, keyboardLayout)} {diagnostics.GetSnapshot()}");
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

            if (imeModule != 0)
            {
                FreeLibrary(imeModule);
            }
        }
    }

    private static void ActivateAndOpenIme(nint edit, nint keyboardLayout)
    {
        ActivateKeyboardLayout(keyboardLayout, 0);
        PumpMessages();

        var activeKeyboardLayout = GetKeyboardLayout(0);
        if (activeKeyboardLayout != keyboardLayout)
        {
            throw new InvalidOperationException(
                $"XiaoXi IME keyboard layout activation did not take effect. ExpectedHkl=0x{keyboardLayout:X}, ActiveHkl=0x{activeKeyboardLayout:X}.");
        }

        var inputContext = ImmGetContext(edit);
        if (inputContext == 0)
        {
            throw new InvalidOperationException(
                $"The integration test EDIT control has no IMM input context. ActiveHkl=0x{activeKeyboardLayout:X}.");
        }

        try
        {
            if (!ImmGetOpenStatus(inputContext) && !ImmSetOpenStatus(inputContext, true))
            {
                throw new Win32Exception(
                    Marshal.GetLastPInvokeError(),
                    $"Unable to open the XiaoXi IME input context. Hkl=0x{activeKeyboardLayout:X}, Himc=0x{inputContext:X}.");
            }

            if (!ImmGetOpenStatus(inputContext))
            {
                throw new InvalidOperationException(
                    $"The XiaoXi IME input context remained closed after ImmSetOpenStatus. Hkl=0x{activeKeyboardLayout:X}, Himc=0x{inputContext:X}.");
            }
        }
        finally
        {
            ImmReleaseContext(edit, inputContext);
        }
    }

    private static string GetImeState(nint edit, nint expectedKeyboardLayout)
    {
        var activeKeyboardLayout = GetKeyboardLayout(0);
        var inputContext = ImmGetContext(edit);
        if (inputContext == 0)
        {
            return $"ExpectedHkl=0x{expectedKeyboardLayout:X}, ActiveHkl=0x{activeKeyboardLayout:X}, Himc=0x0.";
        }

        try
        {
            return $"ExpectedHkl=0x{expectedKeyboardLayout:X}, ActiveHkl=0x{activeKeyboardLayout:X}, Himc=0x{inputContext:X}, ImeOpen={ImmGetOpenStatus(inputContext)}.";
        }
        finally
        {
            ImmReleaseContext(edit, inputContext);
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

    [StructLayout(LayoutKind.Sequential)]
    private struct ImeKeystrokeDiagnosticSnapshot
    {
        public uint Version;
        public uint ImeProcessKeyCallCount;
        public uint ImeToAsciiExCallCount;
        public uint LastProcessVirtualKey;
        public uint LastProcessHandled;
        public uint LastToAsciiVirtualKey;
        public uint LastToAsciiHandled;
        public uint LastCompositionWriteSucceeded;
        public uint LastMessageCount;
        public uint LastReturnValue;

        public override readonly string ToString()
        {
            return $"ImeTraceVersion={Version}, ImeProcessKeyCalls={ImeProcessKeyCallCount}, ImeToAsciiExCalls={ImeToAsciiExCallCount}, LastProcessVk=0x{LastProcessVirtualKey:X}, LastProcessHandled={LastProcessHandled != 0}, LastToAsciiVk=0x{LastToAsciiVirtualKey:X}, LastToAsciiHandled={LastToAsciiHandled != 0}, CompositionWriteSucceeded={LastCompositionWriteSucceeded != 0}, MessageCount={LastMessageCount}, ReturnValue={LastReturnValue}.";
        }
    }

    private sealed class ImeDiagnosticsExports
    {
        private readonly ResetDiagnosticsDelegate _reset;
        private readonly GetDiagnosticsDelegate _getSnapshot;

        private ImeDiagnosticsExports(ResetDiagnosticsDelegate reset, GetDiagnosticsDelegate getSnapshot)
        {
            _reset = reset;
            _getSnapshot = getSnapshot;
        }

        public static ImeDiagnosticsExports Load(nint module)
        {
            var resetAddress = GetProcAddress(module, ResetDiagnosticsExport);
            var getAddress = GetProcAddress(module, GetDiagnosticsExport);
            if (resetAddress == 0 || getAddress == 0)
            {
                throw new InvalidOperationException(
                    $"The installed IME does not expose the current keystroke diagnostics contract. ResetExport=0x{resetAddress:X}, GetExport=0x{getAddress:X}. Rebuild the payload without --no-build and copy the complete new payload to the VM.");
            }

            return new ImeDiagnosticsExports(
                Marshal.GetDelegateForFunctionPointer<ResetDiagnosticsDelegate>(resetAddress),
                Marshal.GetDelegateForFunctionPointer<GetDiagnosticsDelegate>(getAddress));
        }

        public void Reset() => _reset();

        public ImeKeystrokeDiagnosticSnapshot GetSnapshot()
        {
            var snapshot = new ImeKeystrokeDiagnosticSnapshot();
            if (!_getSnapshot(ref snapshot, (uint)Marshal.SizeOf<ImeKeystrokeDiagnosticSnapshot>()))
            {
                throw new InvalidOperationException("The installed IME rejected the keystroke diagnostic snapshot request.");
            }

            return snapshot;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ResetDiagnosticsDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool GetDiagnosticsDelegate(ref ImeKeystrokeDiagnosticSnapshot snapshot, uint snapshotSize);
    }

    [DllImport("user32.dll", EntryPoint = "LoadKeyboardLayoutW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadKeyboardLayout(string keyboardLayoutId, uint flags);

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadLibraryEx(string fileName, nint file, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    private static extern nint GetProcAddress(nint module, string procedureName);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(nint module);

    [DllImport("user32.dll")]
    private static extern nint ActivateKeyboardLayout(nint keyboardLayout, uint flags);

    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint threadId);

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

    [DllImport("imm32.dll")]
    private static extern nint ImmGetContext(nint window);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmReleaseContext(nint window, nint inputContext);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmGetOpenStatus(nint inputContext);

    [DllImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmSetOpenStatus(nint inputContext, [MarshalAs(UnmanagedType.Bool)] bool open);

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
