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

    internal RenderingModeMenu(MediaPlayerControl control, DockPanel layout)
    {
        this.control = control;
        more = new Button { Content = Text("DemoMoreText"), Margin = new Thickness(4), Padding = new Thickness(12, 5, 12, 5) };
        status = new TextBlock { Margin = new Thickness(8), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        var toolbar = new DockPanel();
        DockPanel.SetDock(more, Dock.Right);
        toolbar.Children.Add(more);
        toolbar.Children.Add(status);
        DockPanel.SetDock(toolbar, Dock.Top);
        layout.Children.Add(toolbar);
        layout.Children.Add(control);
        foreach (var mode in Enum.GetValues<VideoRenderingMode>())
        {
            var item = new MenuItem { Header = Text(mode == VideoRenderingMode.Hwnd ? "DemoHwndText" : "DemoD3DImageText"), IsCheckable = true, Tag = mode };
            item.Click += OnSelection;
            items.Add(mode, item);
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = Text("DemoSwitchNoticeText"), IsEnabled = false });
        more.Click += OnMore;
        control.Player.PropertyChanged += OnPlayerChanged;
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
    private void OnPlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
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
        more.IsEnabled = !IsSwitching && control.Player.State != PlaybackState.Opening;
    }

    internal async Task<bool> SwitchAsync(VideoRenderingMode mode)
    {
        if (disposed || IsSwitching || control.Player.State == PlaybackState.Opening) return false;
        var player = control.Player;
        if (mode == control.RenderingMode && player.State != PlaybackState.Failed && recovery == null) { Refresh(); return true; }
        var saved = recovery ?? new Snapshot(player.Source ?? control.Source, player.Position, player.State);
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
            if (saved.Source != null)
            {
                control.SetCurrentValue(MediaPlayerControl.SourceProperty, saved.Source);
                await opened.Task.WaitAsync(deadline.Token);
                deadline.Token.ThrowIfCancellationRequested();
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
            return true;
        }
        catch (Exception error) when (error is ExternalException or InvalidOperationException or ArgumentException or System.IO.IOException or UnauthorizedAccessException or NotSupportedException or OperationCanceledException)
        {
            if (!disposed)
            {
                recovery = saved;
                control.SetCurrentValue(MediaPlayerControl.SourceProperty, null);
                player.Close();
                status.Text = string.Format(Text("DemoSwitchFailedText"), error.Message);
                Trace.WriteLine(error, "Demo.RenderingMode");
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
        more.Click -= OnMore;
        foreach (var item in items.Values) item.Click -= OnSelection;
        control.Player.PropertyChanged -= OnPlayerChanged;
    }

    private sealed record Snapshot(Uri? Source, TimeSpan Position, PlaybackState State);
}
