using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MediaFoundation.Core;
using MediaFoundation.Wpf;
using Player = MediaFoundation.Wpf.MediaPlayer;

namespace MediaFoundation.PlayerDemo;

internal static class SelfTest
{
    internal static async Task RunAsync(Window window, MediaPlayerControl control, Uri source, Action<string> log, CancellationToken ct)
    {
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
        Check(((SolidColorBrush)control.FindResource("PlayerBackgroundBrush")).Color == Color.FromRgb(0x14, 0x26, 0x38), "FindResource 画刷覆盖");
        Check(((SolidColorBrush)control.Background).Color == Color.FromRgb(0x14, 0x26, 0x38), "模板使用应用画刷");
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
        var target = TimeSpan.FromSeconds(Math.Min(4, player.Duration.TotalSeconds / 3));
        progress!.Value = target.TotalSeconds;
        await WaitAsync(() => Math.Abs((player.Position - target).TotalSeconds) < 0.4, "进度条触发 Seek 到目标");
        player.Seek(TimeSpan.FromSeconds(1));
        await WaitAsync(() => Math.Abs(player.Position.TotalSeconds - 1) < 0.4, "公开 API Seek");
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
