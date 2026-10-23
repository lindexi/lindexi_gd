using System.Windows;
using MediaFoundation.Controls;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal static class ScalingTest
{
    internal static async Task RunAsync(Window window, MediaPlayerControl control, Uri source, Action<string> log, CancellationToken ct)
    {
        var player = control.Player;
        async Task WaitAsync(Func<bool> condition)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            while (!condition())
            {
                if (player.LastError != null) throw new InvalidOperationException("Scaling test playback failed", player.LastError);
                await Task.Delay(40, deadline.Token);
            }
        }
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            log("PASS SCALING " + message);
        }
        VideoSurfaceInfo Info() => player.VideoSurfaceInfo ?? throw new InvalidOperationException("Missing surface information");
        void Validate(VideoScalingMode mode, int nativeWidth, int nativeHeight)
        {
            var info = Info();
            Check(info.ScalingMode == mode, $"requested=actual={mode} (no fallback)");
            Check(player.VideoWidth == nativeWidth && player.VideoHeight == nativeHeight, "native metadata unchanged");
            Check(info.DisplayWidth > 0 && info.DisplayHeight > 0 && (info.DisplayWidth != nativeWidth || info.DisplayHeight != nativeHeight), "target display differs from native");
            Check(mode == VideoScalingMode.MixerOutput
                ? info.MixerWidth == info.DisplayWidth && info.MixerHeight == info.DisplayHeight
                : info.MixerWidth == nativeWidth && info.MixerHeight == nativeHeight, "mixer dimensions match selected strategy");
            log($"SURFACE {info}");
        }
        var failures = new List<Exception>();
        foreach (var mode in new[] { VideoScalingMode.PresentationSurface, VideoScalingMode.MixerOutput })
        {
            try
            {
            player.Close();
            control.RenderingMode = VideoRenderingMode.D3DImage;
            control.ScalingMode = mode;
            window.Width = 860; window.Height = 600;
            player.Open(source); player.Play();
            await WaitAsync(() => player.State == PlaybackState.Playing && player.VideoSurfaceInfo is { DisplayWidth: > 0 });
            await Task.Delay(1800, ct);
            int nativeWidth = player.VideoWidth, nativeHeight = player.VideoHeight;
            Validate(mode, nativeWidth, nativeHeight);
            window.Topmost = true; window.Activate();
            VisualTest.Capture(window, $"scaling-{mode}-playing.png");
            player.Pause();
            await WaitAsync(() => player.State == PlaybackState.Paused);
            VisualTest.Capture(window, $"scaling-{mode}-paused-before.png");
            var pausedPosition = player.Position;
            int initialWidth = Info().DisplayWidth;
            window.Width = 700; window.Height = 540;
            await WaitAsync(() => Info().DisplayWidth > 0 && Info().DisplayWidth != initialWidth);
            await Task.Delay(400, ct);
            Validate(mode, nativeWidth, nativeHeight);
            VisualTest.Capture(window, $"scaling-{mode}-paused-resize.png");
            Check(Math.Abs((player.Position - pausedPosition).TotalSeconds) < 0.3, "paused resize preserves clock");
            player.Seek(TimeSpan.FromSeconds(12));
            await Task.Delay(1200, ct);
            Validate(mode, nativeWidth, nativeHeight);
            VisualTest.Capture(window, $"scaling-{mode}-seek.png");
            for (int i = 0; i < 20; i++)
            {
                window.Width = 700 + i % 3 * 50;
                window.Height = 540 + i % 2 * 30;
                await Task.Delay(120, ct);
            }
            await Task.Delay(500, ct);
            Validate(mode, nativeWidth, nativeHeight);
            VisualTest.Capture(window, $"scaling-{mode}-repeat.png");
            player.Close(); player.Open(source); player.Play();
            await WaitAsync(() => player.State == PlaybackState.Playing && player.VideoSurfaceInfo is { DisplayWidth: > 0 });
            await Task.Delay(600, ct);
            Validate(mode, nativeWidth, nativeHeight);
            log($"PASS SCALING {mode} close/reopen");
            for (int i = 0; i < 8; i++)
            {
                window.Width = 720 + i % 3 * 60;
                window.Height = 540 + i % 2 * 40;
                await Task.Delay(250, ct);
            }
            await Task.Delay(500, ct);
            Validate(mode, nativeWidth, nativeHeight);
            VisualTest.Capture(window, $"scaling-{mode}-playing-repeat.png");
            }
            catch (Exception error) when (error is InvalidOperationException or OperationCanceledException or System.Runtime.InteropServices.ExternalException)
            {
                log($"FAIL SCALING case={mode} surface={player.VideoSurfaceInfo} error={error}");
                failures.Add(error);
            }
        }
        if (failures.Count > 0) throw new AggregateException("Scaling cases failed; inspect per-case native and WPF stages", failures);
        player.Close(); control.RenderingMode = VideoRenderingMode.Hwnd;
        player.Open(source); player.Play();
        await WaitAsync(() => player.State == PlaybackState.Playing);
        await Task.Delay(1200, ct);
        Check(player.VideoSurfaceInfo == null, "HWND unchanged: no custom surface");
        VisualTest.Capture(window, "scaling-hwnd.png");
        log("SCALING screenshots require visual inspection; frame counts are not visual assertions. Physical multi-monitor DPI changes are not simulated.");
    }
}
