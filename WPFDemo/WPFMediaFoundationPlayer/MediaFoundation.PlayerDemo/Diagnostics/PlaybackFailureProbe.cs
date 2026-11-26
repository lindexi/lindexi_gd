using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using MediaFoundation.Controls;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class PlaybackFailureProbe
{
    internal static async Task RunAsync(Uri source, VideoRenderingMode failedMode, CancellationToken token)
    {
        var alternateMode = failedMode == VideoRenderingMode.D3DImage ? VideoRenderingMode.Hwnd : VideoRenderingMode.D3DImage;
        string prefix = $"AutoProbe {Guid.NewGuid():N}";
        Log($"begin failedMode={failedMode} {SourceDiagnostics.Describe(source)}");
        try
        {
            if (!source.IsFile)
            {
                Log("skipped: automatic reopen is limited to local files");
                return;
            }

            await ProbeAlternateModeAsync(source, alternateMode, Log, token);
            await ProbeResolverAsync(source, false, Log, token);
            await ProbeResolverAsync(source, true, Log, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Log("canceled");
        }
        finally
        {
            Log("end");
        }

        void Log(string message) => Trace.WriteLine($"{prefix} {message}", "Demo.Diagnostics");
    }

    private static async Task ProbeAlternateModeAsync
        (Uri source, VideoRenderingMode mode, Action<string> log, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string stage = $"alternateMode={mode}";
        log($"{stage} begin (open only, no playback)");
        try
        {
            // A hidden HWND keeps the trial separate from the visible player and its D3DImage.
            using var window = new HwndSource(new HwndSourceParameters("MediaFoundation diagnostic")
                { WindowStyle = unchecked((int)0x80000000), Width = 1, Height = 1 });
            using var session = mode == VideoRenderingMode.Hwnd
                ? new MediaPlaybackSession(window.Handle)
                : MediaPlaybackSession.CreateD3DImagePrototype(window.Handle);
            var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            session.MediaOpened += (_, _) => opened.TrySetResult();
            session.Failed += (_, e) => opened.TrySetException(e.Error);
            session.Open(source);
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(6), token);
            log($"{stage} outcome=opened (does not verify decoded frames)");
        }
        catch (TimeoutException)
        {
            log($"{stage} outcome=timeout");
        }
        catch (Exception error) when (IsProbeFailure(error))
        {
            LogFailure(log, stage, error);
        }
    }

    private static async Task ProbeResolverAsync(Uri source, bool useFileUrl, Action<string> log, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string stage = useFileUrl ? "resolver=file-url" : "resolver=byte-stream";
        log($"{stage} begin");
        try
        {
            string report = await MediaProbe.RunAsync(source, token, useFileUrl);
            log($"{stage} result{Environment.NewLine}{report}");
        }
        catch (Exception error) when (IsProbeFailure(error))
        {
            LogFailure(log, stage, error);
        }
    }

    private static bool IsProbeFailure(Exception error) =>
        error is ExternalException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException;

    private static void LogFailure(Action<string> log, string stage, Exception error) =>
        log($"{stage} outcome=failed HRESULT=0x{error.HResult:X8}{Environment.NewLine}{PlaybackDiagnostics.Sanitize(error.ToString())}");
}
