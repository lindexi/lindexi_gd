using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;
using XiaoXiIme.ImeUi.Avalonia;

if (args.Length == 2 && args[0] == "--product-ui-target")
{
    if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
    Win32ImeEditScenario.RunProductUiTarget(args[1]);
    return 0;
}

var scenarios = new (string Name, Func<Task> Run)[]
{
    ("candidate-window-state", RunCandidateWindowStateAsync),
    ("ime-host-ipc", RunImeHostIpcAsync),
    ("real-ime-keystroke-commit", RunRealImeKeystrokeCommitAsync),
    ("real-ime-composition-lifecycle", RunRealImeCompositionLifecycleAsync),
    ("real-ime-multi-himc-isolation", RunRealImeMultiHimcIsolationAsync),
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
    Ensure(state.CurrentPage == 2 && state.TotalPages == 2, "Candidate page count mismatch.");
    Ensure(state.Candidates.Count == 3, "Candidate count mismatch.");
    Ensure(state.Selection == 10, "Candidate selection mismatch.");
    Ensure(state.Candidates.Single(candidate => candidate.IsSelected).DisplayIndex == 2, "Selected candidate display index mismatch.");
    Ensure(state.AnchorX == 100 && state.AnchorY == 200, "Candidate anchor mismatch.");

    return Task.CompletedTask;
}

static async Task RunImeHostIpcAsync()
{
    var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Integration_{Guid.NewGuid():N}");
    var hostRoot = ResolveHostBaseDirectory();
    using var host = new ImeHostService(options, hostBaseDirectory: hostRoot,
        productSettingsPath: Path.Combine(Path.GetTempPath(), "XiaoXiIme-integration", Guid.NewGuid().ToString("N"), "product-settings.json"));
    using var client = new XiaoXiImeIpcClient(options);

    host.Start();
    var status = await client.WaitUntilReadyAsync();
    await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
    var composingResult = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));
    var uiState = await client.GetUiStateAsync();

    Ensure(status.IsRunning, "IME host should be running.");
    Ensure(!status.IsUsingFallbackDictionary, "IME host should load the production dictionary package from the payload host layout.");
    Ensure(status.DictionaryInputScheme == DictionaryPackageLocations.FullPinyinInputScheme, "IME host should report the full Pinyin input scheme.");
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

static Task RunRealImeCompositionLifecycleAsync()
{
    if (!OperatingSystem.IsWindows())
    {
        throw new PlatformNotSupportedException("The real IME composition lifecycle scenario requires Windows.");
    }

    return Win32ImeEditScenario.RunAsync(lifecycle: true);
}

static Task RunRealImeMultiHimcIsolationAsync()
{
    if (!OperatingSystem.IsWindows())
    {
        throw new PlatformNotSupportedException("The real IME multi-HIMC scenario requires Windows.");
    }

    return Win32ImeEditScenario.RunAsync(multiHimc: true);
}

static string ResolveHostBaseDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var payloadHost = Path.Combine(directory.FullName, "app", "host");
        if (Directory.Exists(Path.Combine(payloadHost, DictionaryPackageLocations.FullPinyinPackageDirectoryName)))
        {
            return payloadHost;
        }

        if (Directory.Exists(Path.Combine(directory.FullName, DictionaryPackageLocations.FullPinyinPackageDirectoryName)))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException(
        "Unable to locate the payload host dictionary layout. Copy XiaoXiIme.DictionaryPackage under app/host before running IntegrationTestHost.");
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
    private const uint WsOverlappedWindow = 0x00CF0000;
    private const uint WsVisible = 0x10000000;
    private const uint WsChild = 0x40000000;
    private const uint WsBorder = 0x00800000;
    private const uint EsAutoHScroll = 0x0080;
    private const int SwShow = 5;
    private const uint PmRemove = 0x0001;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmChar = 0x0102;
    private const uint WmImeStartComposition = 0x010D;
    private const uint WmImeEndComposition = 0x010E;
    private const uint WmImeComposition = 0x010F;
    private const uint LoadLibrarySearchSystem32 = 0x00000800;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint IaceDefault = 0x00000010;
    private const ushort VirtualKeyX = 0x58;
    private const string ResetDiagnosticsExport = "XiaoXiImeResetKeystrokeDiagnostics";
    private const string GetDiagnosticsExport = "XiaoXiImeGetKeystrokeDiagnostics";
    private static readonly InputMessageTrace s_inputMessageTrace = new();

    [DllImport("user32.dll")]
    private static extern bool IsWindow(nint window);

    internal static void RunProductUiTarget(string evidencePath)
    {
        ImmDisableTextFrameService(GetCurrentThreadId());
        var window = CreateWindowEx(0, "STATIC", "XiaoXiIme Product UI EDIT Target", WsOverlappedWindow | WsVisible,
            100, 100, 520, 190, 0, 0, GetModuleHandle(null), 0);
        if (window == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
        nint edit = 0;
        nint context = 0;
        nint previousContext = 0;
        try
        {
            edit = CreateWindowEx(0, "EDIT", string.Empty, WsChild | WsVisible | WsBorder | EsAutoHScroll,
                20, 55, 470, 32, window, 0, GetModuleHandle(null), 0);
            if (edit == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            context = ImmCreateContext();
            if (context == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            previousContext = ImmAssociateContext(edit, context);
            SetForegroundWindow(window);
            SetActiveWindow(window);
            SetFocus(edit);
            PumpMessages();
            WriteSnapshot();
            void WriteSnapshot()
            {
                var associated = ImmGetContext(edit);
                try
                {
                    var length = associated == 0 ? 0 : ImmGetCompositionString(associated, 8, null, 0);
                    var bytes = length > 0 ? new byte[length] : Array.Empty<byte>();
                    if (length > 0) ImmGetCompositionString(associated, 8, bytes, (uint)bytes.Length);
                    File.WriteAllText(evidencePath, System.Text.Json.JsonSerializer.Serialize(new
                    {
                        pid = Environment.ProcessId, window = window.ToInt64(), edit = edit.ToInt64(),
                        thread = GetCurrentThreadId(), himc = associated.ToInt64(), ownedHimc = context.ToInt64(),
                        composition = System.Text.Encoding.Unicode.GetString(bytes)
                    }));
                }
                finally
                {
                    if (associated != 0) ImmReleaseContext(edit, associated);
                }
            }
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(3);
            while (IsWindow(window) && DateTime.UtcNow < deadline)
            {
                PumpMessages();
                if (File.Exists(evidencePath + ".restore"))
                {
                    File.Delete(evidencePath + ".restore");
                    SetForegroundWindow(window);
                    SetActiveWindow(window);
                    SetFocus(edit);
                }
                if (File.Exists(evidencePath + ".snapshot"))
                {
                    WriteSnapshot();
                    File.Delete(evidencePath + ".snapshot");
                }
                if (File.Exists(evidencePath + ".stop")) break;
                Thread.Sleep(10);
            }
        }
        finally
        {
            if (edit != 0 && IsWindow(edit)) ImmAssociateContext(edit, previousContext);
            var contextDestroyed = context == 0 || ImmDestroyContext(context);
            if (IsWindow(window)) DestroyWindow(window);
            if (!contextDestroyed) throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }

    public static async Task RunAsync(bool lifecycle = false, bool multiHimc = false)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The real IME keystroke scenario requires Windows.");
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => RunOnWindowThread(completion, lifecycle, multiHimc))
        {
            IsBackground = true,
            Name = "XiaoXiIme real keystroke integration",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(90));
    }

    private static void RunOnWindowThread(TaskCompletionSource completion, bool lifecycle, bool multiHimc)
    {
        nint window = 0;
        nint keyboardLayout = 0;
        nint imeModule = 0;
        var loadedByImmBeforeProbe = false;
        EditCompositionObserver? observer = null;
        Exception? failure = null;
        try
        {
            WriteStage("entered-window-thread");
            WriteStage("before-ImmDisableTextFrameService");
            var textFrameServiceDisabled = ImmDisableTextFrameService(GetCurrentThreadId());
            WriteStage($"after-ImmDisableTextFrameService Result={textFrameServiceDisabled} Error={Marshal.GetLastPInvokeError()}");

            WriteStage("before-FindInstalledLayoutId");
            var layoutId = FindInstalledLayoutId();
            WriteStage($"after-FindInstalledLayoutId LayoutId={layoutId}");
            WriteStage("before-LoadKeyboardLayout");
            keyboardLayout = LoadKeyboardLayout(layoutId, 0);
            WriteStage($"after-LoadKeyboardLayout Hkl=0x{keyboardLayout:X}");
            if (keyboardLayout == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to load XiaoXi IME keyboard layout {layoutId}.");
            }

            WriteStage("before-create-top-level-window");
            window = CreateWindowEx(
                0,
                "STATIC",
                "XiaoXiIme Automated Keystroke Integration Test",
                WsOverlappedWindow | WsVisible,
                100,
                100,
                520,
                190,
                0,
                0,
                GetModuleHandle(null),
                0);
            WriteStage($"after-create-top-level-window Hwnd=0x{window:X}");
            if (window == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to create the integration test window.");
            }

            var prompt = CreateWindowEx(
                0,
                "STATIC",
                "正在通过 SendInput 注入 xx 并验证输入法上屏",
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

            WriteStage("before-create-EDIT");
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
            WriteStage($"after-create-EDIT Hwnd=0x{edit:X}");
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
                throw new InvalidOperationException("The integration test could not acquire the foreground window and EDIT focus required for injected keyboard input.");
            }

            WriteStage("before-ImmGetDefaultIMEWnd");
            var defaultImeWindow = ImmGetDefaultIMEWnd(edit);
            WriteStage($"after-ImmGetDefaultIMEWnd Hwnd=0x{defaultImeWindow:X}");

            loadedByImmBeforeProbe = GetModuleHandle(ExpectedImeFile) != 0;
            WriteStage("before-LoadLibraryEx");
            imeModule = LoadLibraryEx(ExpectedImeFile, 0, LoadLibrarySearchSystem32);
            WriteStage($"after-LoadLibraryEx Module=0x{imeModule:X}");
            if (imeModule == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to load {ExpectedImeFile} from System32 for keystroke diagnostics.");
            }

            var loadedPathBuffer = new StringBuilder(32768);
            if (GetModuleFileName(imeModule, loadedPathBuffer, loadedPathBuffer.Capacity) == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to identify the loaded IME module.");
            }
            var loadedPath = loadedPathBuffer.ToString();
            using (var loadedFile = File.OpenRead(loadedPath))
            {
                Console.WriteLine($"DIAGNOSTICS loaded-ime-identity: Path={loadedPath}; SHA256={Convert.ToHexString(SHA256.HashData(loadedFile))}; LoadedByImmBeforeProbe={loadedByImmBeforeProbe}");
            }

            WriteStage("before-load-diagnostics-exports");
            var diagnostics = ImeDiagnosticsExports.Load(imeModule);
            WriteStage("after-load-diagnostics-exports");
            Console.WriteLine($"DIAGNOSTICS process-security: {GetProcessSecurityState()}");
            Console.WriteLine($"DIAGNOSTICS ime-module: LoadedByImmBeforeProbe={loadedByImmBeforeProbe}, {GetSignatureState(Path.Combine(Environment.SystemDirectory, ExpectedImeFile))}");
            Console.WriteLine($"DIAGNOSTICS real-ime-keystroke-commit: {GetImeRegistrationState(edit, keyboardLayout)}");
            Console.WriteLine($"DIAGNOSTICS direct-ime-inquire: {CallImeInquire(imeModule)}");
            s_inputMessageTrace.Reset();
            var foregroundSet = SetForegroundWindow(window);
            SetActiveWindow(window);
            SetFocus(edit);
            PumpMessages();
            var foregroundBeforeInput = GetForegroundWindow();
            var focusBeforeInput = GetFocus();
            if (foregroundBeforeInput != window || focusBeforeInput != edit)
            {
                throw new InvalidOperationException($"The target window lost foreground or focus before SendInput. SetForegroundWindow={foregroundSet}, ExpectedForeground=0x{window:X}, ActualForeground=0x{foregroundBeforeInput:X}, ExpectedFocus=0x{edit:X}, ActualFocus=0x{focusBeforeInput:X}.");
            }
            if (multiHimc)
            {
                RunMultiHimcIsolation(window, edit, keyboardLayout);
                var snapshot = diagnostics.GetSnapshot();
                Require(snapshot.ImeProcessKeyCallCount >= 9 && snapshot.ImeToAsciiExCallCount >= 9,
                    $"Multi-HIMC input did not reach the expected native IME call chain. {snapshot}");
                Console.WriteLine($"DIAGNOSTICS real-ime-multi-himc-isolation native-calls: {snapshot}");
                return;
            }

            observer = new EditCompositionObserver(edit, lifecycle);
            if (lifecycle)
            {
                RunCompositionLifecycle(window, edit, keyboardLayout, observer);
                return;
            }

            WriteStage("before-SendInput");
            InjectXxKeystrokes();
            WriteStage("after-SendInput");
            Console.WriteLine($"ACTION real-ime-keystroke-commit: injected xx through SendInput. SetForegroundWindow={foregroundSet}, Foreground=0x{foregroundBeforeInput:X}, Focus=0x{focusBeforeInput:X}.");

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            string text;
            do
            {
                PumpMessages();
                CheckTarget(window, edit, keyboardLayout, observer);
                text = GetWindowText(edit);
                if (text == "小希")
                {
                    var snapshot = diagnostics.GetSnapshot();
                    if (snapshot.ImeProcessKeyCallCount < 2 || snapshot.ImeToAsciiExCallCount < 2)
                    {
                        throw new InvalidOperationException($"The text committed, but the IME call trace was incomplete. {snapshot}");
                    }

                    ObserveStable(window, edit, keyboardLayout, observer, "小希", expectedResults: 1);
                    return;
                }

                Thread.Sleep(10);
            }
            while (DateTime.UtcNow < deadline);

            throw new InvalidOperationException(
                $"Timed out after injecting 'xx' through SendInput. Expected the EDIT control text to be '小希', but it was '{text}'. {GetImeState(edit, keyboardLayout)} {s_inputMessageTrace} {diagnostics.GetSnapshot()}");
        }

        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            observer?.Dispose();
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
            GC.KeepAlive(observer);
            if (failure is not null)
            {
                completion.TrySetException(failure);
            }
            else
            {
                completion.TrySetResult();
            }
        }
    }

    private static void RunMultiHimcIsolation(nint window, nint firstEdit, nint layout)
    {
        var secondEdit = CreateWindowEx(0, "EDIT", string.Empty,
            WsChild | WsVisible | WsBorder | EsAutoHScroll,
            20, 100, 470, 32, window, 0, GetModuleHandle(null), 0);
        Require(secondEdit != 0, "Unable to create the second EDIT.");
        nint firstContext = 0;
        nint secondContext = 0;
        nint firstPrevious = 0;
        nint secondPrevious = 0;
        var contextsAssociated = false;
        EditCompositionObserver? first = null;
        EditCompositionObserver? second = null;
        try
        {
            firstContext = ImmCreateContext();
            secondContext = ImmCreateContext();
            Require(firstContext != 0 && secondContext != 0 && firstContext != secondContext,
                "Two distinct owned HIMCs are required.");
            firstPrevious = ImmAssociateContext(firstEdit, firstContext);
            secondPrevious = ImmAssociateContext(secondEdit, secondContext);
            contextsAssociated = true;
            Require(ImmSetOpenStatus(firstContext, true) && ImmSetOpenStatus(secondContext, true),
                "Unable to open both owned HIMCs.");
            first = new EditCompositionObserver(firstEdit, validateLifecycle: true);
            second = new EditCompositionObserver(secondEdit, validateLifecycle: true);
            Require(first.InputContext == firstContext && second.InputContext == secondContext,
                "EDIT association did not retain the distinct owned HIMCs.");
            using var client = new XiaoXiImeIpcClient();
            var status = WaitWithPump(client.GetHostStatusAsync(), first);
            Require(status.IsRunning && !status.IsUsingFallbackDictionary
                && string.IsNullOrEmpty(status.DictionaryLoadError)
                && !string.IsNullOrWhiteSpace(status.DictionaryPackagePath)
                && Directory.Exists(status.DictionaryPackagePath)
                && status.DictionaryInputScheme == DictionaryPackageLocations.FullPinyinInputScheme,
                $"Multi-HIMC input requires the production fullPinyin external host: {status}");
            var firstSession = ImeSessionId.FromHimc(firstContext);
            var secondSession = ImeSessionId.FromHimc(secondContext);
            CheckHostSnapshot(client, firstSession, first, string.Empty, false);
            CheckHostSnapshot(client, secondSession, second, string.Empty, false);

            Step(firstEdit, first, firstSession, 0x4E, "n");
            Step(secondEdit, second, secondSession, 0x4E, "n");
            CheckFirst("n");
            Step(firstEdit, first, firstSession, 0x49, "ni");
            CheckHostSnapshot(client, secondSession, second, "n", true);
            Require(second.Read(0x0008) == "n" && second.ResultCount == 0,
                "First EDIT update changed the second HIMC.");
            Step(secondEdit, second, secondSession, 0x49, "ni");
            CheckFirst("ni");

            if (Environment.GetEnvironmentVariable("XIAOXIIME_VERIFY_ANCHOR") == "1")
            {
                VerifyCandidate(firstEdit, first, firstSession, "switch-first-no-key");
                VerifyCandidate(secondEdit, second, secondSession, "switch-second-no-key");
                VerifyCandidate(firstEdit, first, firstSession, "switch-back-no-key");
                Require(MoveWindow(window, 230, 180, 520, 190, true), "Unable to move input window.");
                VerifyCandidate(firstEdit, first, firstSession, "moved-no-key");
                var monitor = GetMonitor(firstEdit);
                Require(MoveWindow(window, monitor.Work.Right - 120, monitor.Work.Bottom - 110, 520, 190, true),
                    "Unable to move input window to work area boundary.");
                VerifyCandidate(firstEdit, first, firstSession, "work-area-boundary-no-key");
                Require(MoveWindow(window, 100, 100, 520, 190, true), "Unable to restore input window.");
            }

            SendKey(secondEdit, second, 0x1B);
            WaitFor(() => second.EndCount == 1, second, "second HIMC Escape END");
            Require(second.StartCount == 1 && second.ResultCount == 0
                && second.Read(0x0008) == string.Empty && second.Read(0x0001) == string.Empty
                && second.Read(0x0800) == string.Empty, "Second HIMC cancel must clear strings without a result.");
            CheckHostSnapshot(client, secondSession, second, string.Empty, false);
            ObserveStable(window, secondEdit, layout, second, string.Empty, 0);
            CheckFirst("ni");

            Step(secondEdit, second, secondSession, 0x4E, "n");
            CheckFirst("ni");
            Step(secondEdit, second, secondSession, 0x49, "ni");
            CheckFirst("ni");
            SendKey(secondEdit, second, 0x20);
            WaitFor(() => second.ResultCount == 1 && second.EndCount == 2
                && GetWindowText(secondEdit) == "你", second, "second HIMC Space result/END/text");
            Require(second.StartCount == 2 && second.Results.Single() == "你",
                "Second HIMC must cancel then recompose and commit exactly once.");
            CheckHostSnapshot(client, secondSession, second, string.Empty, false);
            ObserveStable(window, secondEdit, layout, second, "你", 1);
            CheckFirst("ni");

            Focus(firstEdit, first);
            CheckFirst("ni");
            SendKey(firstEdit, first, 0x20);
            WaitFor(() => first.ResultCount == 1 && first.EndCount == 1
                && GetWindowText(firstEdit) == "你", first, "first HIMC Space result/END/text");
            Require(first.StartCount == 1 && first.Results.Single() == "你",
                "First HIMC must retain its original composition and commit once.");
            ObserveStable(window, firstEdit, layout, first, "你", 1);
            CheckHostSnapshot(client, firstSession, first, string.Empty, false);
            CheckHostSnapshot(client, secondSession, second, string.Empty, false);
            Require(GetWindowText(secondEdit) == "你" && second.ResultCount == 1
                && second.StartCount == 2 && second.EndCount == 2,
                "First commit changed the second EDIT lifecycle.");
            Console.WriteLine($"DIAGNOSTICS real-ime-multi-himc-isolation: FirstEDIT=0x{firstEdit:X}, FirstHIMC=0x{firstContext:X}, FirstSession={firstSession}, {first}; SecondEDIT=0x{secondEdit:X}, SecondHIMC=0x{secondContext:X}, SecondSession={secondSession}, {second}");

            void VerifyCandidate(nint edit, EditCompositionObserver observer, ImeSessionId session, string stage)
            {
                Focus(edit, observer);
                WaitFor(() => CandidateMatches(client, edit, observer, session, stage), observer, stage);
                Console.WriteLine($"PASS candidate-position/{stage}");
            }

            void CheckFirst(string reading)
            {
                first.ThrowIfFailed();
                second.ThrowIfFailed();
                Require(first.Read(0x0008) == reading && first.Read(0x0001) == reading
                    && first.StartCount == 1 && first.EndCount == 0 && first.ResultCount == 0,
                    $"Focus switching or second HIMC input changed the first composition '{reading}'. {first}");
                CheckHostSnapshot(client, firstSession, first, reading, true);
            }

            void Focus(nint edit, EditCompositionObserver observer)
            {
                SetForegroundWindow(window);
                SetActiveWindow(window);
                SetFocus(edit);
                PumpMessages();
                first.ThrowIfFailed();
                second.ThrowIfFailed();
                CheckTarget(window, edit, layout, observer);
            }

            void SendKey(nint edit, EditCompositionObserver observer, ushort key)
            {
                Focus(edit, observer);
                Input[] inputs = [Input.Keyboard(key, 0), Input.Keyboard(key, KeyEventKeyUp)];
                Require(SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length,
                    $"Multi-HIMC SendInput did not accept both events for 0x{key:X}.");
            }

            void Step(nint edit, EditCompositionObserver observer, ImeSessionId session, ushort key, string reading)
            {
                var previousUpdates = observer.Updates.Count;
                SendKey(edit, observer, key);
                WaitFor(() => observer.Updates.Skip(previousUpdates).Any(update => update.Composition == reading
                    && update.Reading == reading && update.Cursor == reading.Length), observer, $"multi-HIMC composition {reading}");
                Require(observer.Read(0x0008) == reading && observer.Read(0x0001) == reading,
                    $"Live multi-HIMC composition/reading must be {reading}.");
                CheckHostSnapshot(client, session, observer, reading, true);
            }
        }
        finally
        {
            first?.Dispose();
            second?.Dispose();
            if (contextsAssociated)
            {
                ImmAssociateContext(firstEdit, firstPrevious);
                ImmAssociateContext(secondEdit, secondPrevious);
            }
            var firstDestroyed = firstContext == 0 || ImmDestroyContext(firstContext);
            var secondDestroyed = secondContext == 0 || ImmDestroyContext(secondContext);
            var editDestroyed = DestroyWindow(secondEdit);
            GC.KeepAlive(first);
            GC.KeepAlive(second);
            Console.WriteLine($"DIAGNOSTICS multi-HIMC cleanup: FirstDestroyed={firstDestroyed}, SecondDestroyed={secondDestroyed}, SecondEDITDestroyed={editDestroyed}");
            Require(firstDestroyed && secondDestroyed && editDestroyed, "Multi-HIMC resource cleanup failed.");
        }
    }

    private static void RunCompositionLifecycle(nint window, nint edit, nint layout, EditCompositionObserver observer)
    {
        using var client = new XiaoXiImeIpcClient();
        var status = WaitWithPump(client.GetHostStatusAsync(), observer);
        Console.WriteLine($"DIAGNOSTICS real-ime-composition-lifecycle external-host: {status}");
        Require(status.IsRunning && !status.IsUsingFallbackDictionary
            && string.IsNullOrEmpty(status.DictionaryLoadError)
            && !string.IsNullOrWhiteSpace(status.DictionaryPackagePath)
            && Directory.Exists(status.DictionaryPackagePath)
            && status.DictionaryInputScheme == DictionaryPackageLocations.FullPinyinInputScheme,
            $"Default external host must use the production fullPinyin package without fallback: {status}");
        var session = ImeSessionId.FromHimc(observer.InputContext);
        CheckHostSnapshot(client, session, observer, string.Empty, false);

        Compose();
        SendKey(0x1B);
        WaitFor(() => observer.EndCount == 1, observer, "Escape END");
        Require(observer.StartCount == 1 && observer.ResultCount == 0, "Cancel must have one START/END and no result.");
        Require(observer.Read(0x0008) == string.Empty && observer.Read(0x0001) == string.Empty
            && observer.Read(0x0800) == string.Empty, "Cancel must clear composition/reading/result in the same HIMC.");
        CheckHostSnapshot(client, session, observer, string.Empty, false);
        ObserveStable(window, edit, layout, observer, string.Empty, 0);
        Require(observer.StartCount == 1 && observer.EndCount == 1, "Cancel lifecycle changed during stable observation.");

        Compose();
        SendKey(0x20);
        WaitFor(() => observer.ResultCount == 1 && observer.EndCount == 2 && GetWindowText(edit) == "你",
            observer, "Space result/END/text");
        Require(observer.StartCount == 2 && observer.Results.Single() == "你", "Second composition must start once and commit exactly one result '你'.");
        CheckHostSnapshot(client, session, observer, string.Empty, false);
        ObserveStable(window, edit, layout, observer, "你", 1);
        CheckHostSnapshot(client, session, observer, string.Empty, false);
        Require(observer.StartCount == 2 && observer.EndCount == 2, "Each lifecycle must have exactly one START and END.");
        Console.WriteLine($"DIAGNOSTICS real-ime-composition-lifecycle: HIMC=0x{observer.InputContext:X}, Session={session}, {observer}");

        void Compose()
        {
            Step(0x4E, "n", 1);
            Step(0x49, "ni", 2);
        }

        void Step(ushort key, string composition, int cursor)
        {
            var previousUpdates = observer.Updates.Count;
            SendKey(key);
            WaitFor(() => observer.Updates.Skip(previousUpdates).Any(update => update.Composition == composition
                && update.Reading == composition && update.Cursor == cursor), observer, $"composition {composition}");
            Require(observer.Read(0x0008) == composition && observer.Read(0x0001) == composition,
                $"Live HIMC composition/reading must be {composition}.");
            CheckHostSnapshot(client, session, observer, composition, true);
        }

        void SendKey(ushort key)
        {
            CheckTarget(window, edit, layout, observer);
            Input[] inputs = [Input.Keyboard(key, 0), Input.Keyboard(key, KeyEventKeyUp)];
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            Require(sent == inputs.Length, $"SendInput accepted {sent} of {inputs.Length} events for 0x{key:X}.");
        }
    }

    private static void CheckHostSnapshot(XiaoXiImeIpcClient client, ImeSessionId session,
        EditCompositionObserver observer, string composition, bool composing)
    {
        var snapshot = WaitWithPump(client.GetSnapshotAsync(session), observer);
        if (composing && Environment.GetEnvironmentVariable("XIAOXIIME_VERIFY_ANCHOR") == "1")
        {
            var ui = WaitWithPump(client.GetUiStateAsync(session), observer);
            Require(ui.SessionId == session && ui.Anchor is { IsValid: true } anchor
                && anchor.SessionId == session && anchor.ProcessId == (uint)Environment.ProcessId,
                $"Actual HIMC anchor was not valid for session {session}: {ui.Anchor}");
            Console.WriteLine($"ANCHOR-EVIDENCE session={session} anchor={ui.Anchor}");
        }
        Require(snapshot.IsComposing == composing && snapshot.Composition.DisplayText == composition
            && snapshot.Composition.Reading == composition
            && snapshot.Composition.CaretIndex == composition.Length,
            $"External host snapshot for actual HIMC session {session} did not follow real input: {snapshot}");
        if (composition == "ni")
        {
            Require(snapshot.Candidates.Count > 0 && snapshot.Candidates[0].Text == "你",
                "Production fullPinyin first candidate for ni must be 你.");
        }
    }

    private static bool CandidateMatches(XiaoXiImeIpcClient client, nint edit,
        EditCompositionObserver observer, ImeSessionId session, string stage)
    {
        var ui = WaitWithPump(client.GetUiStateAsync(new ImeUiStateRequest(
            FocusWindowHandle: (long)edit, FocusProcessId: (uint)Environment.ProcessId)), observer);
        Require(ui.SessionId == session && ui.Anchor is { IsValid: true } && ui.Anchor.WindowHandle == (long)edit,
            $"Focus query crossed actual EDIT/session boundary at {stage}: {ui}");
        var candidate = FindWindow(null, "XiaoXiIme Candidate Window");
        if (candidate == 0 || !IsWindowVisible(candidate)) return false;
        Require(GetWindowRect(candidate, out var rect), "Unable to read actual candidate HWND rectangle.");
        var anchor = ui.Anchor!;
        Require(XiaoXiIme.ImeInterop.InputTargetMethods.TryGetPhysicalPoint(edit, anchor.ClientX, anchor.ClientY, out var point),
            "Unable to convert actual EDIT anchor.");
        var top = point.Y + anchor.ExcludeTop - anchor.ScreenY;
        if (anchor.ClientExcludeTop is { } clientTop)
        {
            Require(XiaoXiIme.ImeInterop.InputTargetMethods.TryGetPhysicalPoint(edit, anchor.ClientX, clientTop, out var upper),
                "Unable to convert actual exclusion top.");
            top = upper.Y;
        }
        var work = GetMonitor(edit).Work;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var expectedX = Math.Clamp(point.X, work.Left, Math.Max(work.Left, work.Right - width));
        var expectedY = Math.Clamp(point.Y + height > work.Bottom ? top - height : point.Y,
            work.Top, Math.Max(work.Top, work.Bottom - height));
        var statusPath = Environment.GetEnvironmentVariable("XIAOXIIME_UI_STATUS");
        var status = statusPath is not null && File.Exists(statusPath) ? File.ReadAllText(statusPath) : string.Empty;
        var matches = rect.Left == expectedX && rect.Top == expectedY && width > 0 && height > 0
            && rect.Left >= work.Left && rect.Top >= work.Top && rect.Right <= work.Right && rect.Bottom <= work.Bottom
            && status.StartsWith("visible|ni|", StringComparison.Ordinal)
            && status.Contains($"session={session}|", StringComparison.Ordinal);
        Console.WriteLine($"POSITION-EVIDENCE stage={stage} match={matches} edit=0x{edit:X} himc=0x{observer.InputContext:X} session={session} candidate=0x{candidate:X} rect={rect.Left},{rect.Top},{rect.Right},{rect.Bottom} expected={expectedX},{expectedY} work={work.Left},{work.Top},{work.Right},{work.Bottom} dpi={XiaoXiIme.ImeInterop.InputTargetMethods.GetDpiForWindow(candidate)} anchor={anchor} status={status}");
        return matches;
    }

    private static MonitorInfo GetMonitor(nint window)
    {
        var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        Require(GetMonitorInfo(MonitorFromWindow(window, 2), ref info), "Unable to read actual monitor work area.");
        return info;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public uint Size; public WindowRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string title);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out WindowRect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveWindow(nint window, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    private static T WaitWithPump<T>(Task<T> task, EditCompositionObserver observer)
    {
        WaitFor(() => task.IsCompleted, observer, "read-only default IPC query");
        return task.GetAwaiter().GetResult();
    }

    private static void WaitFor(Func<bool> predicate, EditCompositionObserver observer, string stage)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            PumpMessages();
            observer.ThrowIfFailed();
            if (predicate()) return;
            Thread.Sleep(10);
        }
        while (timer.Elapsed < TimeSpan.FromSeconds(10));
        throw new TimeoutException($"Timed out waiting for {stage}. {observer}");
    }

    private static void ObserveStable(nint window, nint edit, nint layout,
        EditCompositionObserver observer, string expectedText, int expectedResults)
    {
        var timer = Stopwatch.StartNew();
        do
        {
            PumpMessages();
            CheckTarget(window, edit, layout, observer);
            Require(GetWindowText(edit) == expectedText && observer.ResultCount == expectedResults,
                $"Late or duplicate commit: expected '{expectedText}' and {expectedResults} results. {observer}");
            Thread.Sleep(10);
        }
        while (timer.Elapsed < TimeSpan.FromSeconds(1));
    }

    private static void CheckTarget(nint window, nint edit, nint layout, EditCompositionObserver observer)
    {
        observer.ThrowIfFailed();
        Require(GetForegroundWindow() == window && GetFocus() == edit && GetKeyboardLayout(0) == layout,
            "Target foreground/focus/HKL changed.");
        var context = ImmGetContext(edit);
        try
        {
            Require(context != 0 && context == observer.InputContext && ImmGetOpenStatus(context),
                "Target HIMC changed or IME closed.");
        }
        finally
        {
            if (context != 0) ImmReleaseContext(edit, context);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class EditCompositionObserver : IDisposable
    {
        private readonly nint _edit;
        private readonly SubclassProc _callback;
        private Exception? _failure;
        private readonly bool _validateLifecycle;
        private bool _composing;
        private bool _hasResult;
        public nint InputContext { get; }
        public int StartCount { get; private set; }
        public int EndCount { get; private set; }
        public List<string> Results { get; } = [];
        public int ResultCount => Results.Count;
        public List<(string Composition, string Reading, int Cursor)> Updates { get; } = [];

        public EditCompositionObserver(nint edit, bool validateLifecycle)
        {
            _edit = edit;
            _validateLifecycle = validateLifecycle;
            InputContext = ImmGetContext(edit);
            Require(InputContext != 0, "EDIT has no HIMC for synchronous capture.");
            ImmReleaseContext(edit, InputContext);
            _callback = OnMessage;
            Require(SetWindowSubclass(edit, _callback, 1, 0), "Unable to subclass target EDIT.");
        }

        private nint OnMessage(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
        {
            try
            {
                if (message is WmImeStartComposition or WmImeComposition or WmImeEndComposition)
                {
                    if (message == WmImeStartComposition)
                    {
                        Require(!_validateLifecycle || !_composing, "Duplicate START while composing.");
                        StartCount++;
                        _composing = true;
                        _hasResult = false;
                    }
                    if (message == WmImeEndComposition)
                    {
                        Require(!_validateLifecycle || _composing, "END without active composition.");
                        EndCount++;
                        _composing = false;
                    }
                    var context = ImmGetContext(window);
                    try
                    {
                        Require(context != 0 && context == InputContext, "Message HIMC differs from target HIMC.");
                        if (message == WmImeComposition)
                        {
                            var flags = (uint)lParam;
                            if ((flags & 0x0800) != 0)
                            {
                                Require(!_validateLifecycle || (_composing && !_hasResult),
                                    "Result outside composition or repeated result before END.");
                                Results.Add(ReadString(context, 0x0800));
                                _hasResult = true;
                            }
                            if ((flags & (0x0008 | 0x0001 | 0x0080)) != 0)
                            {
                                Require(!_validateLifecycle || _composing, "Composition update outside START/END.");
                                var cursor = ImmGetCompositionString(context, 0x0080, null, 0);
                                Require(cursor >= 0, $"GCS_CURSORPOS failed: {cursor}.");
                                Updates.Add((ReadString(context, 0x0008), ReadString(context, 0x0001), cursor));
                            }
                        }
                    }
                    finally
                    {
                        if (context != 0) ImmReleaseContext(window, context);
                    }
                }
            }
            catch (Exception exception)
            {
                _failure ??= exception;
            }
            return DefSubclassProc(window, message, wParam, lParam);
        }

        public string Read(uint index)
        {
            var context = ImmGetContext(_edit);
            try
            {
                Require(context != 0 && context == InputContext, "Live query HIMC changed.");
                return ReadString(context, index);
            }
            finally
            {
                if (context != 0) ImmReleaseContext(_edit, context);
            }
        }

        private static string ReadString(nint context, uint index)
        {
            var length = ImmGetCompositionString(context, index, null, 0);
            Require(length >= 0 && length % 2 == 0, $"Invalid IMM string length {length} for 0x{index:X}.");
            if (length == 0) return string.Empty;
            var buffer = new byte[length];
            Require(ImmGetCompositionString(context, index, buffer, (uint)length) == length,
                $"IMM string length changed for 0x{index:X}.");
            return Encoding.Unicode.GetString(buffer);
        }

        public void ThrowIfFailed()
        {
            if (_failure is not null) throw new InvalidOperationException($"Synchronous EDIT capture failed. {this}", _failure);
        }

        public void Dispose()
        {
            RemoveWindowSubclass(_edit, _callback, 1);
            GC.KeepAlive(_callback);
        }

        public override string ToString() => $"START={StartCount}, END={EndCount}, Results=[{string.Join(",", Results)}], Updates=[{string.Join(",", Updates)}], CallbackError={_failure}";
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint SubclassProc(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(nint window, SubclassProc callback, nuint id, nuint data);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(nint window, SubclassProc callback, nuint id);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);

    [DllImport("imm32.dll", EntryPoint = "ImmGetCompositionStringW")]
    private static extern int ImmGetCompositionString(nint context, uint index, [Out] byte[]? buffer, uint length);

    private static void ActivateAndOpenIme(nint edit, nint keyboardLayout)
    {
        WriteStage("before-ActivateKeyboardLayout");
        var activatedKeyboardLayout = ActivateKeyboardLayout(keyboardLayout, 0);
        WriteStage($"after-ActivateKeyboardLayout PreviousHkl=0x{activatedKeyboardLayout:X}");
        PumpMessages();

        WriteStage("before-ImmAssociateContextEx");
        var contextAssociated = ImmAssociateContextEx(edit, 0, IaceDefault);
        WriteStage($"after-ImmAssociateContextEx Result={contextAssociated}");
        if (!contextAssociated)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Unable to associate the default IME context after activating the XiaoXi keyboard layout.");
        }
        PumpMessages();

        var activeKeyboardLayout = GetKeyboardLayout(0);
        if (activeKeyboardLayout != keyboardLayout)
        {
            throw new InvalidOperationException(
                $"XiaoXi IME keyboard layout activation did not take effect. ExpectedHkl=0x{keyboardLayout:X}, ActiveHkl=0x{activeKeyboardLayout:X}.");
        }

        WriteStage("before-ImmGetContext");
        var inputContext = ImmGetContext(edit);
        WriteStage($"after-ImmGetContext Himc=0x{inputContext:X}");
        if (inputContext == 0)
        {
            throw new InvalidOperationException(
                $"The integration test EDIT control has no IMM input context. ActiveHkl=0x{activeKeyboardLayout:X}.");
        }

        try
        {
            if (!ImmGetOpenStatus(inputContext))
            {
                WriteStage("before-ImmSetOpenStatus");
                var openStatusSet = ImmSetOpenStatus(inputContext, true);
                WriteStage($"after-ImmSetOpenStatus Result={openStatusSet}");
                if (!openStatusSet)
                {
                    throw new Win32Exception(
                        Marshal.GetLastPInvokeError(),
                        $"Unable to open the XiaoXi IME input context. Hkl=0x{activeKeyboardLayout:X}, Himc=0x{inputContext:X}.");
                }
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

    private static string GetProcessSecurityState()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return $"Name='{identity.Name}', IsAdministrator={principal.IsInRole(WindowsBuiltInRole.Administrator)}, ImpersonationLevel={identity.ImpersonationLevel}, AuthenticationType='{identity.AuthenticationType}'.";
    }

    private static string GetSignatureState(string path)
    {
        try
        {
            using var certificate = X509CertificateLoader.LoadCertificateFromFile(path);
            return $"AuthenticodeSubject='{certificate.Subject}', Thumbprint='{certificate.GetCertHashString(HashAlgorithmName.SHA256)}'.";
        }
        catch (CryptographicException exception)
        {
            return $"AuthenticodeCertificate=None ({exception.HResult:X8}).";
        }
    }

    private static string CallImeInquire(nint module)
    {
        var address = GetProcAddress(module, "ImeInquire");
        if (address == 0)
        {
            return "ImeInquire export was not found.";
        }

        var inquire = Marshal.GetDelegateForFunctionPointer<ImeInquireDelegate>(address);
        var info = new ImeInquireInfo();
        var className = new StringBuilder(80);
        var result = inquire(ref info, className, 0);
        return $"Result={result}, UiClass='{className}', PrivateDataSize={info.PrivateDataSize}, Property=0x{info.Property:X}, ConversionCaps=0x{info.ConversionCaps:X}, SentenceCaps=0x{info.SentenceCaps:X}, UiCaps=0x{info.UiCaps:X}, SetCompCaps=0x{info.SetCompositionStringCaps:X}, SelectCaps=0x{info.SelectCaps:X}.";
    }

    private static string GetImeRegistrationState(nint edit, nint keyboardLayout)
    {
        var fileName = new StringBuilder(260);
        var fileNameLength = ImmGetIMEFileName(keyboardLayout, fileName, (uint)fileName.Capacity);
        var description = new StringBuilder(256);
        var descriptionLength = ImmGetDescription(keyboardLayout, description, (uint)description.Capacity);
        var inputContext = ImmGetContext(edit);
        var conversionMode = 0u;
        var sentenceMode = 0u;
        var conversionStatusRead = inputContext != 0 && ImmGetConversionStatus(inputContext, out conversionMode, out sentenceMode);
        if (inputContext != 0)
        {
            ImmReleaseContext(edit, inputContext);
        }

        return $"ImmIsIME={ImmIsIME(keyboardLayout)}, ImeFile='{fileName}' ({fileNameLength}), Description='{description}' ({descriptionLength}), Property=0x{ImmGetProperty(keyboardLayout, 0x00000004):X}, ConversionCaps=0x{ImmGetProperty(keyboardLayout, 0x00000008):X}, SentenceCaps=0x{ImmGetProperty(keyboardLayout, 0x0000000C):X}, UiCaps=0x{ImmGetProperty(keyboardLayout, 0x00000010):X}, SetCompCaps=0x{ImmGetProperty(keyboardLayout, 0x00000014):X}, SelectCaps=0x{ImmGetProperty(keyboardLayout, 0x00000018):X}, ConversionStatusRead={conversionStatusRead}, ConversionMode=0x{conversionMode:X}, SentenceMode=0x{sentenceMode:X}, DefaultImeWindow=0x{ImmGetDefaultIMEWnd(edit):X}.";
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
                && (string.Equals(imeFile, ExpectedImeFile, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(imeFile, "XiaoXiIme.ime", StringComparison.OrdinalIgnoreCase)))
            {
                return layoutId;
            }
        }

        throw new InvalidOperationException("The installed XiaoXi IME keyboard layout was not found in HKLM.");
    }

    private static void WriteStage(string stage)
    {
        Console.WriteLine($"STAGE real-ime-keystroke-commit: {stage}");
        Console.Out.Flush();
    }

    private static void PumpMessages()
    {
        var budget = Stopwatch.StartNew();
        for (var count = 0; count < 256 && budget.Elapsed < TimeSpan.FromMilliseconds(20)
            && PeekMessage(out var message, 0, 0, 0, PmRemove); count++)
        {
            s_inputMessageTrace.Record(message);
            TranslateMessage(message);
            DispatchMessage(message);
        }
    }

    private static void InjectXxKeystrokes()
    {
        Input[] inputs =
        [
            Input.Keyboard(VirtualKeyX, 0),
            Input.Keyboard(VirtualKeyX, KeyEventKeyUp),
            Input.Keyboard(VirtualKeyX, 0),
            Input.Keyboard(VirtualKeyX, KeyEventKeyUp),
        ];
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"SendInput accepted {sent} of {inputs.Length} keyboard events.");
        }
    }

    private static string GetWindowText(nint window)
    {
        var length = GetWindowTextLength(window);
        var buffer = new StringBuilder(length + 1);
        GetWindowText(window, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private sealed class InputMessageTrace
    {
        private int _keyDownCount;
        private int _keyUpCount;
        private int _charCount;
        private int _imeStartCompositionCount;
        private int _imeCompositionCount;
        private int _imeEndCompositionCount;
        private nint _lastKeyboardWindow;
        private nint _lastImeWindow;

        public void Reset()
        {
            _keyDownCount = 0;
            _keyUpCount = 0;
            _charCount = 0;
            _imeStartCompositionCount = 0;
            _imeCompositionCount = 0;
            _imeEndCompositionCount = 0;
            _lastKeyboardWindow = 0;
            _lastImeWindow = 0;
        }

        public void Record(Message message)
        {
            switch (message.Value)
            {
                case WmKeyDown:
                    _keyDownCount++;
                    _lastKeyboardWindow = message.Window;
                    break;
                case WmKeyUp:
                    _keyUpCount++;
                    _lastKeyboardWindow = message.Window;
                    break;
                case WmChar:
                    _charCount++;
                    _lastKeyboardWindow = message.Window;
                    break;
                case WmImeStartComposition:
                    _imeStartCompositionCount++;
                    _lastImeWindow = message.Window;
                    break;
                case WmImeComposition:
                    _imeCompositionCount++;
                    _lastImeWindow = message.Window;
                    break;
                case WmImeEndComposition:
                    _imeEndCompositionCount++;
                    _lastImeWindow = message.Window;
                    break;
            }
        }

        public override string ToString() =>
            $"MessageTrace KeyDown={_keyDownCount}, KeyUp={_keyUpCount}, Char={_charCount}, ImeStart={_imeStartCompositionCount}, ImeComposition={_imeCompositionCount}, ImeEnd={_imeEndCompositionCount}, LastKeyboardWindow=0x{_lastKeyboardWindow:X}, LastImeWindow=0x{_lastImeWindow:X}.";
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;

        public static Input Keyboard(ushort virtualKey, uint flags) => new()
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = virtualKey,
                    Flags = flags,
                },
            },
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct ImeInquireInfo
    {
        public uint PrivateDataSize;
        public uint Property;
        public uint ConversionCaps;
        public uint SentenceCaps;
        public uint UiCaps;
        public uint SetCompositionStringCaps;
        public uint SelectCaps;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImeKeystrokeDiagnosticSnapshot
    {
        public uint Version;
        public uint ImeInquireCallCount;
        public uint ImeSelectCallCount;
        public uint ImeSetActiveContextCallCount;
        public uint NotifyImeCallCount;
        public uint ImeProcessKeyCallCount;
        public uint ImeToAsciiExCallCount;
        public uint LastProcessVirtualKey;
        public uint LastProcessHandled;
        public uint LastToAsciiVirtualKey;
        public uint LastToAsciiHandled;
        public uint LastCompositionWriteSucceeded;
        public uint LastMessageCount;
        public uint LastReturnValue;
        public uint ProcessKeyHandledCount;
        public uint ToAsciiHandledCount;
        public uint LastProcessError;
        public uint LastToAsciiError;
        public uint LastToAsciiStage;

        public override readonly string ToString()
        {
            return $"ImeTraceVersion={Version}, ImeInquireCalls={ImeInquireCallCount}, ImeSelectCalls={ImeSelectCallCount}, ImeSetActiveContextCalls={ImeSetActiveContextCallCount}, NotifyImeCalls={NotifyImeCallCount}, ImeProcessKeyCalls={ImeProcessKeyCallCount}, ProcessKeyHandledCalls={ProcessKeyHandledCount}, ImeToAsciiExCalls={ImeToAsciiExCallCount}, ToAsciiHandledCalls={ToAsciiHandledCount}, LastProcessVk=0x{LastProcessVirtualKey:X}, LastProcessHandled={LastProcessHandled != 0}, LastProcessError=0x{LastProcessError:X8}, LastToAsciiVk=0x{LastToAsciiVirtualKey:X}, LastToAsciiHandled={LastToAsciiHandled != 0}, LastToAsciiError=0x{LastToAsciiError:X8}, LastToAsciiStage={LastToAsciiStage}, CompositionWriteSucceeded={LastCompositionWriteSucceeded != 0}, MessageCount={LastMessageCount}, ReturnValue={LastReturnValue}.";
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private delegate int ImeInquireDelegate(ref ImeInquireInfo info, StringBuilder className, uint systemInfoFlags);

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

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetModuleFileName(nint module, StringBuilder fileName, int size);

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

    [DllImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmDisableTextFrameService(uint threadId);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmIsIME(nint keyboardLayout);

    [DllImport("imm32.dll", EntryPoint = "ImmGetIMEFileNameW", CharSet = CharSet.Unicode)]
    private static extern uint ImmGetIMEFileName(nint keyboardLayout, StringBuilder fileName, uint bufferLength);

    [DllImport("imm32.dll", EntryPoint = "ImmGetDescriptionW", CharSet = CharSet.Unicode)]
    private static extern uint ImmGetDescription(nint keyboardLayout, StringBuilder description, uint bufferLength);

    [DllImport("imm32.dll")]
    private static extern uint ImmGetProperty(nint keyboardLayout, uint index);

    [DllImport("imm32.dll")]
    private static extern nint ImmGetDefaultIMEWnd(nint window);

    [DllImport("imm32.dll")]
    private static extern nint ImmCreateContext();

    [DllImport("imm32.dll")]
    private static extern nint ImmAssociateContext(nint window, nint inputContext);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmDestroyContext(nint inputContext);

    [DllImport("imm32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmAssociateContextEx(nint window, nint inputContext, uint flags);

    [DllImport("imm32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmGetConversionStatus(nint inputContext, out uint conversionMode, out uint sentenceMode);

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
