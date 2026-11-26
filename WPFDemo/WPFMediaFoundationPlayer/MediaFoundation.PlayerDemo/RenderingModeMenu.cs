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
    private readonly MediaPlayerControl _control;
    private readonly Button _more;
    private readonly TextBlock _status;
    private readonly ContextMenu _menu = new();
    private readonly Dictionary<VideoRenderingMode, MenuItem> _items = new();
    private readonly CancellationTokenSource _lifetime = new();
    private Snapshot? _recovery;
    private bool _disposed;
    private bool _probing;
    private bool _autoProbing;

    internal RenderingModeMenu(MediaPlayerControl control, DockPanel layout)
    {
        _control = control;
        var icon = new System.Windows.Shapes.Path
        {
            Data = System.Windows.Media.Geometry.Parse
            (
                "M3,10 A2,2 0 1 1 3,14 A2,2 0 1 1 3,10 Z M12,10 A2,2 0 1 1 12,14 A2,2 0 1 1 12,10 Z M21,10 A2,2 0 1 1 21,14 A2,2 0 1 1 21,10 Z"
            )
        };
        icon.SetResourceReference(FrameworkElement.StyleProperty, "PlayerIconStyle");
        _more = new Button { Content = icon, ToolTip = Lang.Current.DemoMoreText };
        _more.SetResourceReference(FrameworkElement.StyleProperty, "PlayerButtonStyle");
        System.Windows.Automation.AutomationProperties.SetName(_more, Lang.Current.DemoMoreText);
        var header = new FrameworkElementFactory(typeof(TextBlock));
        header.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
        header.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        header.SetValue(FrameworkElement.MaxWidthProperty, 500d);
        var menuItemStyle = new Style(typeof(MenuItem));
        menuItemStyle.Setters.Add
            (new Setter(HeaderedItemsControl.HeaderTemplateProperty, new DataTemplate { VisualTree = header }));
        _menu.Resources.Add(typeof(MenuItem), menuItemStyle);
        _more.ContextMenu = _menu;
        control.ToolbarContent = _more;
        _status = new TextBlock
        {
            Margin = new Thickness(8), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
        };
        _status.SetResourceReference(TextBlock.ForegroundProperty, "PlayerForegroundBrush");
        var notice = new Border { Child = _status };
        var noticeStyle = new Style(typeof(Border));
        var emptyNotice = new DataTrigger
            { Binding = new System.Windows.Data.Binding(nameof(TextBlock.Text)) { Source = _status }, Value = "" };
        emptyNotice.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        noticeStyle.Triggers.Add(emptyNotice);
        notice.Style = noticeStyle;
        notice.SetResourceReference(Border.BackgroundProperty, "PlayerBackgroundBrush");
        DockPanel.SetDock(notice, Dock.Bottom);
        layout.Children.Add(notice);
        layout.Children.Add(control);
        foreach (var mode in Enum.GetValues<VideoRenderingMode>())
        {
            var item = new MenuItem
            {
                Header = mode == VideoRenderingMode.Hwnd ? Lang.Current.DemoHwndText : Lang.Current.DemoD3DImageText,
                IsCheckable = true, Tag = mode
            };
            item.Click += OnSelectionAsync;
            _items.Add(mode, item);
            _menu.Items.Add(item);
        }

        _menu.Items.Add(new Separator());
        foreach (var scaling in Enum.GetValues<VideoScalingMode>())
        {
            var item = new MenuItem
            {
                Header = scaling == VideoScalingMode.MixerOutput ? Lang.Current.DemoScalingMixerText : Lang.Current.DemoScalingSurfaceText,
                IsCheckable = true
            };
            _menu.Opened += (_, _) =>
            {
                item.IsChecked = control.ScalingMode == scaling;
                item.IsEnabled = !IsSwitching;
            };
            item.Click += async (_, _) => await SwitchAsync(VideoRenderingMode.D3DImage, scaling);
            _menu.Items.Add(item);
        }

        _menu.Items.Add(new Separator());
        var prototype = new MenuItem { Header = Lang.Current.DemoMFPlayPrototypeText };
        prototype.Click += async (_, _) => await OpenPrototypeAsync();
        _menu.Items.Add(prototype);
        var openLogs = new MenuItem { Header = Lang.Current.DemoOpenLogsText };
        openLogs.Click += (_, _) => DiagnosticAction
        (() => Process.Start
            (
                new ProcessStartInfo(System.IO.Path.GetDirectoryName(PlaybackDiagnostics.LogPath)!)
                    { UseShellExecute = true }
            )
        );
        var copy = new MenuItem { Header = Lang.Current.DemoCopyDiagnosticsText };
        copy.Click += (_, _) => DiagnosticAction
        (() => Clipboard.SetText
            (
                Lang.Current.DemoLocalLogPrivacyText + Environment.NewLine + PlaybackDiagnostics.Summary +
                Environment.NewLine +
                _status.Text
            )
        );
        var probe = new MenuItem { Header = Lang.Current.DemoMediaProbeText };
        probe.Click += async (_, _) => await ProbeAsync();
        _menu.Items.Add(probe);
        _menu.Items.Add(openLogs);
        _menu.Items.Add(copy);
        _menu.Items.Add(new MenuItem { Header = Lang.Current.DemoLocalLogPrivacyText, IsEnabled = false });
        _menu.Items.Add(new MenuItem { Header = Lang.Current.DemoSwitchNoticeText, IsEnabled = false });
        _more.Click += OnMore;
        control.Player.PropertyChanged += OnPlayerChanged;
        control.Player.Failed += OnPlaybackFailedAsync;
        Refresh();
    }

    internal bool IsSwitching { get; private set; }
    internal string ErrorText => _status.Text;

    private void OnMore(object sender, RoutedEventArgs e)
    {
        Refresh();
        _menu.PlacementTarget = _more;
        _menu.Placement = PlacementMode.Bottom;
        _menu.IsOpen = true;
    }

    private async void OnSelectionAsync(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await SwitchAsync((VideoRenderingMode)((MenuItem)sender).Tag);
    }

    private async Task OpenPrototypeAsync()
    {
        try
        {
            var source = _control.Source;
            var owner = Window.GetWindow(_control);
            if (source == null)
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = Lang.Current.FileFilter,
                    CheckFileExists = true
                };
                if (dialog.ShowDialog(owner) != true) return;
                source = new Uri(dialog.FileName);
            }

            Trace.WriteLine
                ($"PROTOTYPE stage=gui-launch renderingModeUnchanged={_control.RenderingMode}", "MFPlay.Prototype");
            await MFPlayPrototype.RunAsync(source.IsFile ? source.LocalPath : source.AbsoluteUri, false, owner);
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or System.IO.IOException
                                          or UnauthorizedAccessException or ArgumentException)
        {
            PlaybackDiagnostics.Write(error, _control.Source);
            _status.Text = Lang.Current.DemoDiagnosticActionFailedText.ToString
            (
                PlaybackDiagnostics.Sanitize(error.Message),
                PlaybackDiagnostics.LogPath
            );
        }
    }

    private void DiagnosticAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or System.IO.IOException
                                          or UnauthorizedAccessException)
        {
            PlaybackDiagnostics.Write(error, _control.Player.Source);
            _status.Text = Lang.Current.DemoDiagnosticActionFailedText.ToString
            (
                PlaybackDiagnostics.Sanitize(error.Message),
                PlaybackDiagnostics.LogPath
            );
        }
    }

    private async void OnPlaybackFailedAsync(object? sender, PlaybackFailedEventArgs e)
    {
        var source = _control.Player.Source ?? _control.Source;
        var failedMode = _control.RenderingMode;
        string path = PlaybackDiagnostics.Write(e.Error, source);
        var native = e.Error;
        while (native.InnerException != null) native = native.InnerException;
        _status.Text = Lang.Current.DemoPlaybackFailedText.ToString
        (
            $"{PlaybackDiagnostics.Sanitize(e.Error.Message)} (0x{native.HResult:X8})",
            path
        );
        _status.ToolTip = _status.Text;
        if (_disposed || source == null) return;
        if (_autoProbing)
        {
            Trace.WriteLine($"AutoProbe skipped: already running failedMode={failedMode} {SourceDiagnostics.Describe(source)}",
                "Demo.Diagnostics");
            return;
        }

        _autoProbing = true;
        try
        {
            var token = _lifetime.Token;
            // Let the failed player finish releasing its renderer before opening a diagnostic session.
            await Task.Yield();
            await PlaybackFailureProbe.RunAsync(source, failedMode, token);
        }
        finally
        {
            _autoProbing = false;
        }
    }

    private async Task ProbeAsync()
    {
        var source = _control.Player.Source ?? _control.Source;
        if (source == null || _disposed || _probing) return;
        _probing = true;
        PlaybackDiagnostics.RegisterSource(source);
        try
        {
            string report = await MediaProbe.RunAsync(source, _lifetime.Token);
            Trace.WriteLine
                ($"MediaProbe {SourceDiagnostics.Describe(source)}{Environment.NewLine}{report}", "Demo.Diagnostics");
            if (_disposed || source != (_control.Player.Source ?? _control.Source)) return;
            _status.Text = Lang.Current.DemoMediaProbeResultText.ToString(report, PlaybackDiagnostics.LogPath);
            if (_control.Player.State == PlaybackState.Failed)
                _status.Text = Lang.Current.DemoProbePlaybackStillFailedText + Environment.NewLine + _status.Text;
            _status.ToolTip = _status.Text;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception error) when (error is ExternalException or System.IO.IOException or UnauthorizedAccessException
                                          or ArgumentException or InvalidOperationException)
        {
            PlaybackDiagnostics.Write(error, source);
            if (!_disposed)
                _status.Text = Lang.Current.DemoDiagnosticActionFailedText.ToString
                (
                    PlaybackDiagnostics.Sanitize(error.Message),
                    PlaybackDiagnostics.LogPath
                );
        }
        finally
        {
            _probing = false;
        }
    }

    private void OnPlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MediaFoundation.Controls.MediaPlayer.Source) or null or "")
            PlaybackDiagnostics.RegisterSource(_control.Player.Source ?? _control.Source);
        if (!IsSwitching && _recovery != null && _control.Player.Source != null)
        {
            _recovery = null;
            _status.Text = "";
        }

        Refresh();
    }

    private void Refresh()
    {
        foreach (var pair in _items)
        {
            pair.Value.IsChecked = pair.Key == _control.RenderingMode;
            pair.Value.IsEnabled = !IsSwitching && _control.Player.State != PlaybackState.Opening;
        }

        _more.IsEnabled = true;
    }

    internal async Task<bool> SwitchAsync(VideoRenderingMode mode, VideoScalingMode? scaling = null)
    {
        if (_disposed || IsSwitching || _control.Player.State == PlaybackState.Opening) return false;
        var player = _control.Player;
        if (mode == _control.RenderingMode && (!scaling.HasValue || scaling.Value == _control.ScalingMode) &&
            player.State != PlaybackState.Failed && _recovery == null)
        {
            Refresh();
            return true;
        }

        var saved = _recovery ?? new Snapshot(player.Source ?? _control.Source, player.Position, player.State);
        PlaybackDiagnostics.RegisterSource(saved.Source);
        Trace.WriteLine
        (
            $"Switch begin from={_control.RenderingMode} to={mode} state={saved.State} position={saved.Position} rate={player.PlaybackRate}",
            "Demo.RenderingMode"
        );
        bool autoPlay = _control.AutoPlay;
        IsSwitching = true;
        _control.IsEnabled = false;
        _status.Text = Lang.Current.DemoSwitchingText;
        Refresh();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnOpened(object? sender, EventArgs e) => opened.TrySetResult();
        void OnFailed(object? sender, PlaybackFailedEventArgs e) => opened.TrySetException(e.Error);
        player.MediaOpened += OnOpened;
        player.Failed += OnFailed;
        try
        {
            _control.AutoPlay = false;
            _control.SetCurrentValue(MediaPlayerControl.SourceProperty, null);
            player.Close(); // Also handles media opened directly through Player rather than Source.
            _control.RenderingMode = mode;
            if (scaling.HasValue) _control.ScalingMode = scaling.Value;
            Trace.WriteLine($"Switch scaling={_control.ScalingMode} backend=MFPlay", "Demo.RenderingMode");
            if (saved.Source != null)
            {
                _control.SetCurrentValue(MediaPlayerControl.SourceProperty, saved.Source);
                await opened.Task.WaitAsync(deadline.Token);
                deadline.Token.ThrowIfCancellationRequested();
                Trace.WriteLine
                    ($"Switch restore mode={mode} state={saved.State} position={saved.Position}", "Demo.RenderingMode");
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

            _recovery = null;
            _status.Text = "";
            Trace.WriteLine
                ($"Switch complete mode={mode} state={player.State} position={player.Position}", "Demo.RenderingMode");
            return true;
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException
                                          or System.IO.IOException or UnauthorizedAccessException
                                          or NotSupportedException or OperationCanceledException)
        {
            if (!_disposed)
            {
                _recovery = saved;
                Trace.WriteLine
                (
                    $"Switch failed mode={mode} state={player.State} position={player.Position}", "Demo.RenderingMode"
                );
                string logPath = PlaybackDiagnostics.Write(error, saved.Source);
                var native = error;
                while (native.InnerException != null) native = native.InnerException;
                _status.Text = Lang.Current.DemoSwitchFailedText.ToString
                (
                    $"{PlaybackDiagnostics.Sanitize(error.Message)} (0x{native.HResult:X8})", logPath
                );
                _status.ToolTip = _status.Text;
                _control.SetCurrentValue(MediaPlayerControl.SourceProperty, null);
                player.Close();
            }

            return false;
        }
        finally
        {
            player.MediaOpened -= OnOpened;
            player.Failed -= OnFailed;
            if (!_disposed)
            {
                _control.AutoPlay = autoPlay;
                _control.IsEnabled = true;
                IsSwitching = false;
                Refresh();
            }
        }
    }

    private async Task WaitStateAsync(PlaybackState state, CancellationToken token)
    {
        while (_control.Player.State != state)
        {
            token.ThrowIfCancellationRequested();
            if (_control.Player.LastError is { } error) throw new InvalidOperationException(error.Message, error);
            await Task.Delay(20, token);
        }
    }

    /// <summary>释放菜单订阅并取消诊断和模式切换。</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _menu.IsOpen = false;
        if (ReferenceEquals(_control.ToolbarContent, _more)) _control.ToolbarContent = null;
        _more.Click -= OnMore;
        foreach (var item in _items.Values) item.Click -= OnSelectionAsync;
        _control.Player.PropertyChanged -= OnPlayerChanged;
        _control.Player.Failed -= OnPlaybackFailedAsync;
    }

    private sealed record Snapshot(Uri? Source, TimeSpan Position, PlaybackState State);
}
