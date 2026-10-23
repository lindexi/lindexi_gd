using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;

namespace MediaSamples;

internal static unsafe class DesktopRunner
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
    private static LRESULT ProcessMessage(HWND window, uint message, WPARAM first, LPARAM second)
        => PInvoke.DefWindowProc(window, message, first, second);

    private static int TestNoCamera(HWND window, string path)
    {
        uint count = MediaSession.CountCaptureDevices();
        Console.WriteLine($"Capture device enumeration: videoDevices={count}; process={RuntimeInformation.ProcessArchitecture}");
        if (count != 0)
        {
            Console.Error.WriteLine("FAIL: expected no camera, but video capture devices were enumerated.");
            return 4;
        }

        MediaSession? session = null;
        bool expectedFailure = false;
        try
        {
            session = new MediaSession(window, SampleKind.CaptureEngine, path);
            var clock = Stopwatch.StartNew();
            while (clock.Elapsed < TimeSpan.FromSeconds(12))
            {
                session.CheckCaptureInitialization();
                if (session.CaptureInitialized)
                {
                    Console.Error.WriteLine("FAIL: capture initialization succeeded; expected no camera.");
                    return 4;
                }
                Thread.Sleep(2);
            }
            session.CheckCaptureInitialization();
            Console.Error.WriteLine("FAIL: timed out waiting for the expected no-camera initialization error.");
            return 4;
        }
        catch (COMException error) when (error.HResult == HRESULT.MF_E_NO_CAPTURE_DEVICES_AVAILABLE.Value)
        {
            expectedFailure = true;
        }
        finally
        {
            session?.Dispose();
        }
        if (expectedFailure)
        {
            Console.WriteLine($"PASS expected-no-camera HRESULT=0x{unchecked((uint)HRESULT.MF_E_NO_CAPTURE_DEVICES_AVAILABLE.Value):X8}");
            return 0;
        }
        return 4;
    }

    internal static int Run(string[] args, SampleKind kind)
    {
        int exitCode = RunCore(args, kind);
        Console.WriteLine($"RESULT kind={kind} exitCode={exitCode}");
        return exitCode;
    }

    private static int RunCore(string[] args, SampleKind kind)
    {
        bool selfTest = args.Contains("--self-test");
        bool expectNoCamera = args.Contains("--expect-no-camera");
        if (expectNoCamera && (!selfTest || kind != SampleKind.CaptureEngine))
        {
            Console.Error.WriteLine("FAIL: --expect-no-camera requires CaptureEngine and --self-test.");
            return 2;
        }
        string path = args.FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))
            ?? Path.Combine(AppContext.BaseDirectory, "Video", "视频.mp4");
        HWND window = default;
        bool initialized = false;
        try
        {
            PInvoke.CoInitializeEx(null, COINIT.COINIT_MULTITHREADED).ThrowOnFailure();
            initialized = true;
            if (kind != SampleKind.CaptureEngine && !File.Exists(path)) throw new FileNotFoundException("Video source not found.", path);
            var module = PInvoke.GetModuleHandle((PCWSTR)null);
            fixed (char* className = "CsWin32MediaSample")
            fixed (char* title = kind.ToString())
            {
                var windowClass = new WNDCLASSW { lpfnWndProc = &ProcessMessage, hInstance = module, lpszClassName = className };
                if (PInvoke.RegisterClass(&windowClass) == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                window = PInvoke.CreateWindowEx(0, className, title, WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE,
                    100, 100, 800, 500, default, default, module, null);
            }
            if (window.IsNull) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            if (expectNoCamera) return TestNoCamera(window, path);
            using var session9 = kind == SampleKind.SourceReader9 ? new SourceReader9Session(window, path) : null;
            using var session = kind != SampleKind.SourceReader9 ? new MediaSession(window, kind, path) : null;
            object gate = session9?.Gate ?? session!.Gate;
            var clock = Stopwatch.StartNew();
            bool resized = false;
            while (true)
            {
                MSG message;
                while (PInvoke.PeekMessage(&message, default, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
                {
                    if (message.message == 0x12) return selfTest ? 2 : 0;
                    if (message.message == 0x10) goto Finished;
                    PInvoke.TranslateMessage(&message);
                    PInvoke.DispatchMessage(&message);
                }
                if (selfTest && !resized && clock.Elapsed >= TimeSpan.FromSeconds(4))
                {
                    if (!PInvoke.MoveWindow(window, 100, 100, 960, 640, true)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    resized = true;
                }
                lock (gate)
                {
                    if (session9 != null) session9.Tick();
                    else session!.Tick();
                }
                if (selfTest && clock.Elapsed >= TimeSpan.FromSeconds(12)) break;
                Thread.Sleep(2);
            }
        Finished:
            long frames = session9?.Frames ?? session!.Frames;
            Console.WriteLine($"RESULT kind={kind} frames={frames} elapsed={clock.Elapsed} selfTest={selfTest}");
            if (selfTest && frames < 3)
            {
                Console.Error.WriteLine("FAIL: fewer than three frames were actually presented.");
                return 3;
            }
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL kind={kind}: {error}");
            return 1;
        }
        finally
        {
            if (!window.IsNull) PInvoke.DestroyWindow(window);
            if (initialized) PInvoke.CoUninitialize();
        }
    }
}
