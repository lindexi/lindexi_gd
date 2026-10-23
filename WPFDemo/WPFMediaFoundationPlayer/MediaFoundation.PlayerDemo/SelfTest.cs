using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MediaFoundation.Core;
using MediaFoundation.Controls;
using Player = MediaFoundation.Controls.MediaPlayer;

namespace MediaFoundation.PlayerDemo;

internal static class SelfTest
{
    internal static async Task RunDiagnosticsAsync()
    {
        var resources = new System.Resources.ResourceManager("MediaFoundation.Controls.Strings", typeof(MediaPlayerControl).Assembly);
        var filter = resources.GetString("FileFilter") ?? throw new InvalidOperationException("Missing FileFilter resource");
        var parts = filter.Split('|');
        if (parts.Length != 8 || parts.Any(string.IsNullOrWhiteSpace) || parts[7] != "*.*")
            throw new InvalidOperationException("Invalid media/video/audio/all-files filter format");
        var video = parts[3].Split(';');
        var audio = parts[5].Split(';');
        var media = parts[1].Split(';');
        string[] requiredVideo = ["*.mp4", "*.wmv", "*.avi", "*.mov", "*.m4v", "*.mkv", "*.webm", "*.asf", "*.mts", "*.m2ts", "*.ts", "*.mpg", "*.mpeg", "*.vob", "*.3gp", "*.3g2"];
        string[] requiredAudio = ["*.mp3", "*.wav", "*.wma", "*.aac", "*.flac", "*.m4a", "*.ogg", "*.opus", "*.aiff", "*.aif"];
        if (requiredVideo.Except(video).Any() || requiredAudio.Except(audio).Any())
            throw new InvalidOperationException("Missing video/audio file extensions");
        if (!media.ToHashSet().SetEquals(video.Concat(audio)) || media.Any(pattern => !pattern.StartsWith("*.", StringComparison.Ordinal) || pattern.Length <= 2))
            throw new InvalidOperationException("Invalid combined media filter patterns");
        _ = new Microsoft.Win32.OpenFileDialog { Filter = filter };
        Console.WriteLine("PASS file filter format/categories/extensions including mkv/all-files (not decoder support)");
        var source = new Uri(@"C:\private-media\diagnostic-secret.mp4");
        using var registrations = new System.IO.StringWriter();
        using var registrationListener = new TextWriterTraceListener(registrations);
        Trace.Listeners.Add(registrationListener);
        try
        {
            PlaybackDiagnostics.RegisterSource(source);
            for (int i = 0; i < 100; i++)
                PlaybackDiagnostics.RegisterSource(new Uri(source.AbsoluteUri));
            Parallel.For(0, 100, _ => PlaybackDiagnostics.RegisterSource(new Uri(source.AbsoluteUri.ToUpperInvariant())));
        }
        finally { Trace.Listeners.Remove(registrationListener); }
        if (registrations.ToString().Split("RegisterSource sourceId=", StringSplitOptions.None).Length != 2)
            throw new InvalidOperationException("Duplicate source registration");
        if (PlaybackDiagnostics.Sanitize(source.LocalPath) != source.LocalPath)
            throw new InvalidOperationException("Local media path removed");
        if (PlaybackDiagnostics.Sanitize(source.AbsoluteUri) != source.AbsoluteUri)
            throw new InvalidOperationException("Local file URI removed");
        if (PlaybackDiagnostics.Sanitize("https://user:password@example.com/media?token=secret").Contains("secret", StringComparison.Ordinal))
            throw new InvalidOperationException("Network credentials leaked");
        if (SourceDiagnostics.Describe(source).Contains("diagnostic-secret", StringComparison.Ordinal))
            throw new InvalidOperationException("Source description leaked media name");
        var error = new InvalidOperationException("diagnostic failure " + source.LocalPath,
            new System.Runtime.InteropServices.COMException("stage=DiagnosticProbe", unchecked((int)0x887A0004)));
        string path = PlaybackDiagnostics.Write(error, source);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        string content;
        do
        {
            await Task.Delay(100, deadline.Token);
            content = "";
            if (System.IO.File.Exists(path))
            {
                using var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                using var reader = new System.IO.StreamReader(stream);
                content = await reader.ReadToEndAsync(deadline.Token);
            }
        } while (!content.Contains("stage=DiagnosticProbe", StringComparison.Ordinal));
        if (!content.Contains("Startup process=", StringComparison.Ordinal)) throw new InvalidOperationException("Missing normal startup diagnostics");
        if (!content.Contains("HRESULT=0x887A0004", StringComparison.Ordinal)) throw new InvalidOperationException("Missing inner HRESULT");
        if (!content.Contains("System.InvalidOperationException", StringComparison.Ordinal)) throw new InvalidOperationException("Missing full exception");
        if (!content.Contains(path, StringComparison.Ordinal)) throw new InvalidOperationException("Missing log path");
        if (!content.Contains("sourcePath=" + source.LocalPath, StringComparison.Ordinal)) throw new InvalidOperationException("Missing local source path");
        Console.WriteLine("PASS diagnostics startup/exception/HRESULT/path/privacy/registration-deduplication " + path);
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "diagnostics-result.log"), content);
    }

    internal static async Task RunRenderingMenuAsync(MediaPlayerControl control, RenderingModeMenu menu, Uri source, Action<string> log, CancellationToken ct)
    {
        var initialMode = control.RenderingMode;
        async Task CheckSwitchAsync(VideoRenderingMode mode)
        {
            if (!await menu.SwitchAsync(mode)) throw new InvalidOperationException(menu.ErrorText);
            if (control.RenderingMode != mode || control.Player.RenderingMode != mode || menu.IsSwitching)
                throw new InvalidOperationException("Rendering mode/control/menu mismatch.");
        }
        await CheckSwitchAsync(VideoRenderingMode.D3DImage);
        await CheckSwitchAsync(VideoRenderingMode.Hwnd);
        if (control.Source != null || control.Player.Source != null || control.Player.State != PlaybackState.Closed)
            throw new InvalidOperationException("Unopened mode selection must not open media.");
        log("PASS unopened control RenderingMode D3DImage/Hwnd, synchronized and Closed");
        control.AutoPlay = false;
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnOpened(object? sender, EventArgs e) => opened.TrySetResult();
        control.Player.MediaOpened += OnOpened;
        try
        {
            control.Source = source;
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        }
        finally { control.Player.MediaOpened -= OnOpened; }
        control.Player.Play();
        async Task WaitAsync(Func<bool> condition)
        {
            var clock = Stopwatch.StartNew();
            while (!condition())
            {
                if (control.Player.LastError is { } error) throw new InvalidOperationException("Menu test playback", error);
                if (clock.Elapsed > TimeSpan.FromSeconds(12)) throw new TimeoutException("Menu test native state confirmation");
                await Task.Delay(20, ct);
            }
        }
        await WaitAsync(() => control.Player.State == PlaybackState.Playing && control.Player.Position > TimeSpan.FromSeconds(0.7));
        control.Player.Pause();
        await WaitAsync(() => control.Player.State == PlaybackState.Paused);
        var position = control.Player.Position;
        bool switched = await menu.SwitchAsync(VideoRenderingMode.D3DImage);
        if (switched)
        {
            await WaitAsync(() => control.Player.State == PlaybackState.Paused && Math.Abs((control.Player.Position - position).TotalSeconds) < 0.6);
            log("PASS menu D3DImage reopen and paused position restoration (not frame-presentation proof)");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(menu.ErrorText) || menu.IsSwitching) throw new InvalidOperationException("Mode failure must expose an error and unlock menu.");
            throw new InvalidOperationException("D3DImage paused switch: " + menu.ErrorText);
        }
        await CheckSwitchAsync(VideoRenderingMode.Hwnd);
        await WaitAsync(() => control.Player.State == PlaybackState.Paused && Math.Abs((control.Player.Position - position).TotalSeconds) < 0.6);
        if (control.Source != source || control.Player.Source != source || control.AutoPlay)
            throw new InvalidOperationException("Reopen must preserve source synchronization and AutoPlay.");
        log("PASS menu media return/recovery to HWND, paused intent, position, Source and AutoPlay");
        foreach (var intent in new[] { PlaybackState.Playing, PlaybackState.Stopped })
        {
            control.AutoPlay = true;
            if (intent == PlaybackState.Playing) control.Player.Play();
            else control.Player.Stop();
            await WaitAsync(() => control.Player.State == intent);
            var switching = menu.SwitchAsync(VideoRenderingMode.D3DImage);
            if (!switching.IsCompleted && await menu.SwitchAsync(VideoRenderingMode.Hwnd))
                throw new InvalidOperationException("Concurrent mode switch must be rejected.");
            if (!await switching) throw new InvalidOperationException("D3DImage " + intent + ": " + menu.ErrorText);
            await WaitAsync(() => control.Player.State == intent);
            log("PASS menu D3DImage restoration intent=" + intent);
            await CheckSwitchAsync(VideoRenderingMode.Hwnd);
            await WaitAsync(() => control.Player.State == intent);
            if (!control.AutoPlay || control.Source != source || control.Player.Source != source)
                throw new InvalidOperationException("Mode switch lost AutoPlay or source.");
            if (intent == PlaybackState.Stopped && control.Player.Position > TimeSpan.FromMilliseconds(100))
                throw new InvalidOperationException("Stopped reopen must remain at the start.");
            log("PASS menu HWND recovery intent=" + intent + ", AutoPlay restored, rapid request isolated");
        }
        control.AutoPlay = false;
        control.Source = null;
        control.Player.Close();
        if (initialMode != VideoRenderingMode.Hwnd) await CheckSwitchAsync(initialMode);
    }

    internal static async Task RunAsync(Window window, MediaPlayerControl control, Uri source, Action<string> log, CancellationToken ct)
    {
        using (var deferred = new Player())
        {
            var stableView = deferred.VideoView;
            deferred.Open(source);
            bool modeRejected = false;
            try { deferred.RenderingMode = VideoRenderingMode.D3DImage; }
            catch (InvalidOperationException) { modeRejected = true; }
            if (!modeRejected) throw new InvalidOperationException("Deferred Open must lock RenderingMode.");
            deferred.Close();
            deferred.RenderingMode = VideoRenderingMode.D3DImage;
            deferred.RenderingMode = VideoRenderingMode.Hwnd;
            if (!ReferenceEquals(stableView, deferred.VideoView)) throw new InvalidOperationException("VideoView must remain stable.");
            log("PASS deferred Open mode lock, Close unlock, stable VideoView");
        }
        using var callbackCounts = new CallbackCounts();
        Trace.Listeners.Add(callbackCounts);
        var player = control.Player;
        int opened = 0, ended = 0, failures = 0, notifications = 0;
        bool expectedFailure = false;
        int unexpectedFailures = 0;
        player.MediaOpened += (_, _) => { opened++; log($"EVENT MediaOpened #{opened}"); };
        player.PlaybackEnded += (_, _) => { ended++; log($"EVENT EOF #{ended}"); };
        player.PropertyChanged += (_, _) => notifications++;
        player.Failed += (_, e) => { failures++; if (!expectedFailure) unexpectedFailures++; log($"EVENT Failed expected={expectedFailure}: {e.Error}"); };
        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            log("PASS " + name);
        }
        async Task WaitAsync(Func<bool> condition, string name, int seconds = 12)
        {
            var clock = Stopwatch.StartNew();
            while (!condition())
            {
                ct.ThrowIfCancellationRequested();
                if (player.LastError != null && !expectedFailure) throw new InvalidOperationException(name, player.LastError);
                if (clock.Elapsed > TimeSpan.FromSeconds(seconds)) throw new TimeoutException(name);
                await Task.Delay(40, ct);
            }
            log("PASS " + name);
        }
        async Task OpenAsync()
        {
            int previous = opened;
            player.Open(source);
            await WaitAsync(() => opened == previous + 1, "异步打开完成");
        }
        async Task PauseAsync()
        {
            player.Pause();
            await WaitAsync(() => player.State == PlaybackState.Paused, "原生暂停确认");
        }
        log($"START {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} STA={Thread.CurrentThread.GetApartmentState()} source={source}");
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        control.ApplyTemplate();
        Check(control.Template != null, "默认控件模板加载（未重写）");
        Check(((SolidColorBrush)control.FindResource("PlayerBackgroundBrush")).Color == Color.FromRgb(0x13, 0x18, 0x20), "FindResource 画刷覆盖");
        Check(((SolidColorBrush)control.Background).Color == Color.FromRgb(0x13, 0x18, 0x20), "模板使用应用画刷");
        var buttons = Descendants(control).OfType<Button>().ToArray();
        var style = (Style)control.FindResource("PlayerButtonStyle");
        Check(buttons.Length >= 6 && buttons.All(b => ReferenceEquals(b.Style, style) && b.MinHeight == 40), "基础 Button style 覆盖真实模板按钮");
        var progress = control.Template!.FindName("PART_Progress", control) as Slider;
        Check(progress != null, "PART_Progress 模板部件");
        await OpenAsync();
        Check(player.HasAudio, "已选择音轨 HasAudio=true（不等同于可听声音）");
        Check(player.HasVideo, "已选择视频轨");
        Check(player.Duration > TimeSpan.FromSeconds(3), $"有效时长 {player.Duration}");
        Check(player.VideoWidth > 0 && player.VideoHeight > 0, $"原生尺寸 {player.VideoWidth}x{player.VideoHeight}");
        await VisualTest.RunAsync(window, control, log, ct);
        MediaPlayerControl.PlayPauseCommand.Execute(null, control);
        for (int resize = 0; resize < 80; resize++)
        {
            window.Width = 900 + resize % 7 * 13;
            window.Height = 600 + resize % 5 * 11;
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            if (player.LastError != null) throw new InvalidOperationException("Resize/play stress", player.LastError);
            await Task.Delay(10, ct);
        }
        await WaitAsync(() => player.State == PlaybackState.Playing && player.Position > TimeSpan.FromSeconds(0.7), "控件播放命令推进原生时钟");
        MediaPlayerControl.PlayPauseCommand.Execute(null, control);
        await WaitAsync(() => player.State == PlaybackState.Paused, "控件暂停命令");
        await Task.Delay(300, ct);
        var paused = player.Position;
        await Task.Delay(700, ct);
        Check(Math.Abs((player.Position - paused).TotalMilliseconds) < 100, "暂停位置保持");
        await VisualTest.RunProgressAsync(control, log, ct);
        var target = TimeSpan.FromSeconds(Math.Min(4, player.Duration.TotalSeconds / 3));
        progress!.Value = target.TotalSeconds;
        await WaitAsync(() => Math.Abs((player.Position - target).TotalSeconds) < 0.4, "进度条触发 Seek 到目标");
        player.Seek(TimeSpan.FromSeconds(1));
        await WaitAsync(() => Math.Abs(player.Position.TotalSeconds - 1) < 0.4, "公开 API Seek");
        player.Seek(TimeSpan.FromSeconds(6));
        player.Seek(TimeSpan.FromSeconds(2));
        player.Seek(TimeSpan.FromSeconds(5));
        await WaitAsync(() => Math.Abs(player.Position.TotalSeconds - 5) < 0.4, "暂停连续 Seek 保留最后目标");
        await Task.Delay(500, ct);
        Check(player.State == PlaybackState.Paused && Math.Abs(player.Position.TotalSeconds - 5) < 0.4, "暂停连续 Seek 后位置稳定");
        player.Play();
        await WaitAsync(() => player.State == PlaybackState.Playing && player.Position.TotalSeconds > 5.4, "连续 Seek 后真实 PLAYING 与时钟推进");
        await PauseAsync();
        if (Environment.GetCommandLineArgs().Contains("--seek-regression"))
        {
            player.Close();
            await OpenAsync();
            player.Play();
            await WaitAsync(() => player.State == PlaybackState.Playing && player.Position > TimeSpan.FromSeconds(0.4), "focused close-reopen clock progression");
            player.Close();
            log($"PASS focused {player.RenderingMode} pause/continuous-seek/resume/close-reopen (rate sweep not requested)");
            return;
        }
        MediaPlayerControl.StopCommand.Execute(null, control);
        await WaitAsync(() => player.State == PlaybackState.Stopped && player.Position < TimeSpan.FromMilliseconds(100), "Stop 原生确认并归零");
        player.Volume = 0.35;
        Check(player.Volume == 0.35, "Volume 设置 0.35 原生命令无异常");
        MediaPlayerControl.MuteCommand.Execute(null, control);
        Check(player.IsMuted, "Mute 命令");
        MediaPlayerControl.MuteCommand.Execute(null, control);
        Check(!player.IsMuted, "取消静音");
        LoopbackProbe? probe = null;
        try { probe = new LoopbackProbe(); }
        catch (System.Runtime.InteropServices.COMException error) when (error.HResult is unchecked((int)0x80070490) or unchecked((int)0x88890004) or unchecked((int)0x88890010))
        {
            log($"EXPECTED AudioEnvironmentUnavailable HRESULT=0x{error.HResult:X8}; actual audio output NOT VERIFIED (not PASS)");
        }
        if (probe != null)
        {
            using (probe)
            {
                player.Play();
                await WaitAsync(() => player.State == PlaybackState.Playing, "loopback playback started");
                for (int sample = 0; sample < 100; sample++)
                {
                    await Task.Delay(30, ct);
                    probe.Poll();
                }
                Check(probe.Samples > 0 && probe.Rms > 0.00001, $"WASAPI loopback output samples={probe.Samples} RMS={probe.Rms:G6}");
                await PauseAsync();
            }
        }
        try
        {
            player.PlaybackRate = 2;
            player.Play();
            await WaitAsync(() => player.State == PlaybackState.Playing, "倍速播放确认");
            await Task.Delay(1500, ct);
            var start = player.Position;
            var measurement = Stopwatch.StartNew();
            await Task.Delay(3000, ct);
            var advanced = (player.Position - start).TotalSeconds / measurement.Elapsed.TotalSeconds;
            Check(advanced > 1.6 && advanced < 2.4, $"2 倍速原生时钟推进比率 {advanced:F3}");
            await PauseAsync();
        }
        catch (NotSupportedException error)
        {
            log($"EXPECTED NativeUnsupported 倍速：{error}");
        }
        player.PlaybackRate = 1;
        var size = (window.Width, window.Height);
        window.Width = 900; window.Height = 640;
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        var original = (window.WindowStyle, window.ResizeMode, window.WindowState);
        MediaPlayerControl.FullscreenCommand.Execute(null, control);
        Check(control.IsFullscreen && window.WindowStyle == WindowStyle.None, "进入全屏");
        MediaPlayerControl.FullscreenCommand.Execute(null, control);
        Check(!control.IsFullscreen && original == (window.WindowStyle, window.ResizeMode, window.WindowState), "全屏还原窗口属性");
        window.Width = size.Width; window.Height = size.Height;
        MediaPlayerControl.LoopCommand.Execute(null, control);
        Check(player.IsLooping, "循环命令");
        player.Seek(player.Duration - TimeSpan.FromSeconds(1.5));
        await WaitAsync(() => player.Position > player.Duration - TimeSpan.FromSeconds(2), "定位到真实 EOF 前");
        player.Play();
        await WaitAsync(() => ended > 0, "真实原生 EOF 事件", 18);
        await WaitAsync(() => player.State == PlaybackState.Playing && player.Position > TimeSpan.FromSeconds(0.2) && player.Position < TimeSpan.FromSeconds(3), "循环 EOF 后从头推进");
        player.IsLooping = false;
        await OpenAsync();
        player.Close();
        await Task.Delay(300, ct);
        int handlesBefore = Process.GetCurrentProcess().HandleCount;
        for (int cycle = 0; cycle < 8; cycle++)
        {
            await OpenAsync();
            player.Play();
            await WaitAsync(() => player.State == PlaybackState.Playing, "repeat open/play");
            player.Close();
            await Task.Delay(150, ct);
        }
        int handlesAfter = Process.GetCurrentProcess().HandleCount;
        Check(handlesAfter - handlesBefore < 40, $"Native handles before={handlesBefore} after={handlesAfter} cycles=8 tolerance=39");
        player.Close();
        Check(player.State == PlaybackState.Closed && player.Source == null, "Close 释放并重置");
        await OpenAsync();
        Check(player.Volume == 0.35 && !player.IsMuted && player.PlaybackRate == 1, "重开保留设置");
        expectedFailure = true;
        int oldFailures = failures;
        player.Open(new Uri(System.IO.Path.Combine(AppContext.BaseDirectory, "missing-" + Guid.NewGuid() + ".mp4")));
        await WaitAsync(() => failures == oldFailures + 1 && player.State == PlaybackState.Failed, "错误文件触发 Failed");
        Check(player.LastError != null, "保留真实错误");
        expectedFailure = false;
        await OpenAsync();
        player.Play();
        await WaitAsync(() => player.State == PlaybackState.Playing, "错误恢复后播放");
        control.Dispose();
        int snapshot = notifications;
        await Task.Delay(700, ct);
        Check(notifications == snapshot, "Dispose 后无轮询或迟到用户通知");
        bool rejected = false;
        try { player.Play(); } catch (ObjectDisposedException) { rejected = true; }
        Check(rejected, "Dispose 后拒绝操作");
        control.Dispose();
        await WaitAsync(() => callbackCounts.Allocated == callbackCounts.Released, "callback allocated/released balanced");
        Check(callbackCounts.Allocated > 0, $"callback allocated={callbackCounts.Allocated} released={callbackCounts.Released}");
        Check(unexpectedFailures == 0, $"unexpected Failed events={unexpectedFailures}");
        Trace.Listeners.Remove(callbackCounts);
        log("PASS FUNCTIONAL/LIFECYCLE (audio output status is reported separately; not proof of absence of all native leaks)");
    }

    private sealed class CallbackCounts : TraceListener
    {
        private int allocated, released;
        internal int Allocated => Volatile.Read(ref allocated);
        internal int Released => Volatile.Read(ref released);
        public override void Write(string? message) { }
        public override void WriteLine(string? message)
        {
            if (message?.Contains("MFPlay callback allocated live=", StringComparison.Ordinal) == true) Interlocked.Increment(ref allocated);
            if (message?.Contains("MFPlay callback released live=", StringComparison.Ordinal) == true) Interlocked.Increment(ref released);
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
