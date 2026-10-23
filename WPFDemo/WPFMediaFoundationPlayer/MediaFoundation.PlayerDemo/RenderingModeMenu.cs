using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MediaFoundation.Controls;
using MediaFoundation.Core;

namespace MediaFoundation.PlayerDemo;

internal sealed class RenderingModeMenu : IDisposable
{
    private readonly MediaPlayerControl control;
    private readonly Button more;
    private readonly TextBlock status;
    private readonly ContextMenu menu = new();
    private readonly Dictionary<VideoRenderingMode, MenuItem> items = new();
    private readonly CancellationTokenSource lifetime = new();
    private Snapshot? recovery;
    private bool disposed;
    private bool probing;

    internal RenderingModeMenu(MediaPlayerControl control, DockPanel layout)
    {
        this.control = control;
        var icon = new System.Windows.Shapes.Path
        {
            Data = System.Windows.Media.Geometry.Parse("M3,10 A2,2 0 1 1 3,14 A2,2 0 1 1 3,10 Z M12,10 A2,2 0 1 1 12,14 A2,2 0 1 1 12,10 Z M21,10 A2,2 0 1 1 21,14 A2,2 0 1 1 21,10 Z")
        };
        icon.SetResourceReference(FrameworkElement.StyleProperty, "PlayerIconStyle");
        more = new Button { Content = icon, ToolTip = Text("DemoMoreText") };
        more.SetResourceReference(FrameworkElement.StyleProperty, "PlayerButtonStyle");
        System.Windows.Automation.AutomationProperties.SetName(more, Text("DemoMoreText"));
        var header = new FrameworkElementFactory(typeof(TextBlock));
        header.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
        header.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        header.SetValue(FrameworkElement.MaxWidthProperty, 500d);
        var menuItemStyle = new Style(typeof(MenuItem));
        menuItemStyle.Setters.Add(new Setter(HeaderedItemsControl.HeaderTemplateProperty, new DataTemplate { VisualTree = header }));
        menu.Resources.Add(typeof(MenuItem), menuItemStyle);
        more.ContextMenu = menu;
        control.ToolbarContent = more;
        status = new TextBlock { Margin = new Thickness(8), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        status.SetResourceReference(TextBlock.ForegroundProperty, "PlayerForegroundBrush");
        var notice = new Border { Child = status };
        var noticeStyle = new Style(typeof(Border));
        var emptyNotice = new DataTrigger { Binding = new System.Windows.Data.Binding(nameof(TextBlock.Text)) { Source = status }, Value = "" };
        emptyNotice.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        noticeStyle.Triggers.Add(emptyNotice);
        notice.Style = noticeStyle;
        notice.SetResourceReference(Border.BackgroundProperty, "PlayerBackgroundBrush");
        DockPanel.SetDock(notice, Dock.Bottom);
        layout.Children.Add(notice);
        layout.Children.Add(control);
        foreach (var mode in Enum.GetValues<VideoRenderingMode>())
        {
            var item = new MenuItem { Header = Text(mode == VideoRenderingMode.Hwnd ? "DemoHwndText" : "DemoD3DImageText"), IsCheckable = true, Tag = mode };
            item.Click += OnSelection;
            items.Add(mode, item);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        foreach (var scaling in Enum.GetValues<VideoScalingMode>())
        {
            var item = new MenuItem { Header = Text(scaling == VideoScalingMode.MixerOutput ? "DemoScalingMixerText" : "DemoScalingSurfaceText"), IsCheckable = true };
            menu.Opened += (_, _) => { item.IsChecked = control.ScalingMode == scaling; item.IsEnabled = !IsSwitching; };
            item.Click += async (_, _) => await SwitchAsync(VideoRenderingMode.D3DImage, scaling);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var prototype = new MenuItem { Header = Text("DemoMfPlayPrototypeText") };
        prototype.Click += async (_, _) => await OpenPrototypeAsync();
        menu.Items.Add(prototype);
        var openLogs = new MenuItem { Header = Text("DemoOpenLogsText") };
        openLogs.Click += (_, _) => DiagnosticAction(() => Process.Start(new ProcessStartInfo(System.IO.Path.GetDirectoryName(PlaybackDiagnostics.LogPath)!) { UseShellExecute = true }));
        var copy = new MenuItem { Header = Text("DemoCopyDiagnosticsText") };
        copy.Click += (_, _) => DiagnosticAction(() => Clipboard.SetText(Text("DemoLocalLogPrivacyText") + Environment.NewLine + PlaybackDiagnostics.Summary + Environment.NewLine + status.Text));
        var probe = new MenuItem { Header = Text("DemoMediaProbeText") };
        probe.Click += async (_, _) => await ProbeAsync();
        menu.Items.Add(probe);
        menu.Items.Add(openLogs);
        menu.Items.Add(copy);
        menu.Items.Add(new MenuItem { Header = Text("DemoLocalLogPrivacyText"), IsEnabled = false });
        menu.Items.Add(new MenuItem { Header = Text("DemoSwitchNoticeText"), IsEnabled = false });
        more.Click += OnMore;
        control.Player.PropertyChanged += OnPlayerChanged;
        control.Player.Failed += OnPlaybackFailed;
        Refresh();
    }

    internal bool IsSwitching { get; private set; }
    internal string ErrorText => status.Text;
    private static string Text(string key) => (string)Application.Current.FindResource(key);
    private void OnMore(object sender, RoutedEventArgs e)
    {
        Refresh();
        menu.PlacementTarget = more;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }
    private async void OnSelection(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await SwitchAsync((VideoRenderingMode)((MenuItem)sender).Tag);
    }
    private async Task OpenPrototypeAsync()
    {
        try
        {
            var source = control.Source;
            var owner = Window.GetWindow(control);
            if (source == null)
            {
                var resources = new System.Resources.ResourceManager("MediaFoundation.Controls.Strings", typeof(MediaPlayerControl).Assembly);
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = resources.GetString("FileFilter") ?? throw new InvalidOperationException(),
                    CheckFileExists = true
                };
                if (dialog.ShowDialog(owner) != true) return;
                source = new Uri(dialog.FileName);
            }
            Trace.WriteLine($"PROTOTYPE stage=gui-launch renderingModeUnchanged={control.RenderingMode}", "MFPlay.Prototype");
            await MfPlayPrototype.RunAsync(source.IsFile ? source.LocalPath : source.AbsoluteUri, false, owner);
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or ArgumentException)
        {
            PlaybackDiagnostics.Write(error, control.Source);
            status.Text = string.Format(Text("DemoDiagnosticActionFailedText"), PlaybackDiagnostics.Sanitize(error.Message), PlaybackDiagnostics.LogPath);
        }
    }

    private void DiagnosticAction(Action action)
    {
        try { action(); }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or System.IO.IOException or UnauthorizedAccessException)
        {
            PlaybackDiagnostics.Write(error, control.Player.Source);
            status.Text = string.Format(Text("DemoDiagnosticActionFailedText"), PlaybackDiagnostics.Sanitize(error.Message), PlaybackDiagnostics.LogPath);
        }
    }
    private async void OnPlaybackFailed(object? sender, PlaybackFailedEventArgs e)
    {
        string path = PlaybackDiagnostics.Write(e.Error, control.Player.Source ?? control.Source);
        var native = e.Error;
        while (native.InnerException != null) native = native.InnerException;
        status.Text = string.Format(Text("DemoPlaybackFailedText"), $"{PlaybackDiagnostics.Sanitize(e.Error.Message)} (0x{native.HResult:X8})", path);
        status.ToolTip = status.Text;
        await ProbeAsync();
    }

    private async Task ProbeAsync()
    {
        var source = control.Player.Source ?? control.Source;
        if (source == null || disposed || probing) return;
        probing = true;
        PlaybackDiagnostics.RegisterSource(source);
        try
        {
            string report = await MediaProbe.RunAsync(source, lifetime.Token);
            Trace.WriteLine($"MediaProbe {SourceDiagnostics.Describe(source)}{Environment.NewLine}{report}", "Demo.Diagnostics");
            if (disposed || source != (control.Player.Source ?? control.Source)) return;
            status.Text = string.Format(Text("DemoMediaProbeResultText"), report, PlaybackDiagnostics.LogPath);
            if (control.Player.State == PlaybackState.Failed) status.Text = Text("DemoProbePlaybackStillFailedText") + Environment.NewLine + status.Text;
            status.ToolTip = status.Text;
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is ExternalException or System.IO.IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            PlaybackDiagnostics.Write(error, source);
            if (!disposed) status.Text = string.Format(Text("DemoDiagnosticActionFailedText"), PlaybackDiagnostics.Sanitize(error.Message), PlaybackDiagnostics.LogPath);
        }
        finally { probing = false; }
    }

    private void OnPlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MediaFoundation.Controls.MediaPlayer.Source) or null or "")
            PlaybackDiagnostics.RegisterSource(control.Player.Source ?? control.Source);
        if (!IsSwitching && recovery != null && control.Player.Source != null)
        {
            recovery = null;
            status.Text = "";
        }
        Refresh();
    }
    private void Refresh()
    {
        foreach (var pair in items)
        {
            pair.Value.IsChecked = pair.Key == control.RenderingMode;
            pair.Value.IsEnabled = !IsSwitching && control.Player.State != PlaybackState.Opening;
        }
        more.IsEnabled = true;
    }

    internal async Task<bool> SwitchAsync(VideoRenderingMode mode, VideoScalingMode? scaling = null)
    {
        if (disposed || IsSwitching || control.Player.State == PlaybackState.Opening) return false;
        var player = control.Player;
        if (mode == control.RenderingMode && (!scaling.HasValue || scaling.Value == control.ScalingMode) && player.State != PlaybackState.Failed && recovery == null) { Refresh(); return true; }
        var saved = recovery ?? new Snapshot(player.Source ?? control.Source, player.Position, player.State);
        PlaybackDiagnostics.RegisterSource(saved.Source);
        Trace.WriteLine($"Switch begin from={control.RenderingMode} to={mode} state={saved.State} position={saved.Position} rate={player.PlaybackRate}", "Demo.RenderingMode");
        bool autoPlay = control.AutoPlay;
        IsSwitching = true;
        control.IsEnabled = false;
        status.Text = Text("DemoSwitchingText");
        Refresh();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnOpened(object? sender, EventArgs e) => opened.TrySetResult();
        void OnFailed(object? sender, PlaybackFailedEventArgs e) => opened.TrySetException(e.Error);
        player.MediaOpened += OnOpened;
        player.Failed += OnFailed;
        try
        {
            control.AutoPlay = false;
            control.SetCurrentValue(MediaPlayerControl.SourceProperty, null);
            player.Close(); // Also handles media opened directly through Player rather than Source.
            control.RenderingMode = mode;
            if (scaling.HasValue) control.ScalingMode = scaling.Value;
            Trace.WriteLine($"Switch scaling={control.ScalingMode} backend=MFPlay", "Demo.RenderingMode");
            if (saved.Source != null)
            {
                control.SetCurrentValue(MediaPlayerControl.SourceProperty, saved.Source);
                await opened.Task.WaitAsync(deadline.Token);
                deadline.Token.ThrowIfCancellationRequested();
                Trace.WriteLine($"Switch restore mode={mode} state={saved.State} position={saved.Position}", "Demo.RenderingMode");
                if (saved.Position > TimeSpan.Zero && player.CanSeek) player.Seek(saved.Position);
                if (saved.State is PlaybackState.Playing or PlaybackState.Paused)
                {
                    // HWND queues Play behind its in-flight seek; do not infer completion from Position.
                    player.Play();
                    await WaitStateAsync(PlaybackState.Playing, deadline.Token);
                    if (saved.State == PlaybackState.Paused)
                    {
                        player.Pause();
                        await WaitStateAsync(PlaybackState.Paused, deadline.Token);
                    }
                }
            }
            recovery = null;
            status.Text = "";
            Trace.WriteLine($"Switch complete mode={mode} state={player.State} position={player.Position}", "Demo.RenderingMode");
            return true;
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException or System.IO.IOException or UnauthorizedAccessException or NotSupportedException or OperationCanceledException)
        {
            if (!disposed)
            {
                recovery = saved;
                Trace.WriteLine($"Switch failed mode={mode} state={player.State} position={player.Position}", "Demo.RenderingMode");
                string logPath = PlaybackDiagnostics.Write(error, saved.Source);
                var native = error;
                while (native.InnerException != null) native = native.InnerException;
                status.Text = string.Format(Text("DemoSwitchFailedText"), $"{PlaybackDiagnostics.Sanitize(error.Message)} (0x{native.HResult:X8})", logPath);
                status.ToolTip = status.Text;
                control.SetCurrentValue(MediaPlayerControl.SourceProperty, null);
                player.Close();
            }
            return false;
        }
        finally
        {
            player.MediaOpened -= OnOpened;
            player.Failed -= OnFailed;
            if (!disposed)
            {
                control.AutoPlay = autoPlay;
                control.IsEnabled = true;
                IsSwitching = false;
                Refresh();
            }
        }
    }

    private async Task WaitStateAsync(PlaybackState state, CancellationToken token)
    {
        while (control.Player.State != state)
        {
            token.ThrowIfCancellationRequested();
            if (control.Player.LastError is { } error) throw new InvalidOperationException(error.Message, error);
            await Task.Delay(20, token);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        lifetime.Dispose();
        menu.IsOpen = false;
        if (ReferenceEquals(control.ToolbarContent, more)) control.ToolbarContent = null;
        more.Click -= OnMore;
        foreach (var item in items.Values) item.Click -= OnSelection;
        control.Player.PropertyChanged -= OnPlayerChanged;
        control.Player.Failed -= OnPlaybackFailed;
    }

    private sealed record Snapshot(Uri? Source, TimeSpan Position, PlaybackState State);
}
