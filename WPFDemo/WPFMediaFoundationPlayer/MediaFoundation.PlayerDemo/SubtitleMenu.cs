using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MediaFoundation.Controls;
using MediaFoundation.Core;
using Microsoft.Win32;

namespace MediaFoundation.PlayerDemo;

internal sealed class SubtitleMenu : IDisposable
{
    private readonly MediaPlayerControl _control;
    private readonly ContextMenu _menu = new();
    private readonly MenuItem _load;
    private readonly MenuItem _embedded;
    private readonly MenuItem _readEmbedded;
    private bool _readingEmbedded;
    private int _sourceVersion;
    private Uri? _observedSource;
    private readonly MenuItem _clear;
    private readonly MenuItem _earlier;
    private readonly MenuItem _later;
    private readonly MenuItem _reset;
    private readonly CancellationTokenSource _lifetime = new();
    private int _mediaVersion;
    private bool _loading;
    private bool _disposed;

    internal SubtitleMenu(MediaPlayerControl control)
    {
        _control = control;
        _observedSource = control.Player.Source;
        var root = new MenuItem { Header = Text("SubtitleMenu") };
        _load = new MenuItem { Header = Text("SubtitleLoad") };
        _clear = new MenuItem { Header = Text("SubtitleClear") };
        _earlier = new MenuItem { Header = Text("SubtitleEarlier") };
        _later = new MenuItem { Header = Text("SubtitleLater") };
        _reset = new MenuItem { Header = Text("SubtitleReset") };
        _embedded = new MenuItem { Header = Text("SubtitleEmbedded") };
        _readEmbedded = new MenuItem { Header = Text("SubtitleReadEmbedded") };
        _readEmbedded.Click += OnReadEmbeddedAsync;
        _embedded.Items.Add(_readEmbedded);
        _load.Click += OnLoadAsync;
        _clear.Click += (_, _) => control.Player.Subtitle = null;
        _earlier.Click += (_, _) => control.Player.SubtitleDelay -= TimeSpan.FromMilliseconds(500);
        _later.Click += (_, _) => control.Player.SubtitleDelay += TimeSpan.FromMilliseconds(500);
        _reset.Click += (_, _) => control.Player.SubtitleDelay = TimeSpan.Zero;
        root.Items.Add(_load);
        root.Items.Add(_embedded);
        root.Items.Add(_clear);
        root.Items.Add(new Separator());
        root.Items.Add(_earlier);
        root.Items.Add(_later);
        root.Items.Add(_reset);
        _menu.Items.Add(root);
        _menu.Opened += (_, _) => Refresh();
        control.ContextMenu = _menu;
        control.Player.PropertyChanged += OnChanged;
        control.Player.SubtitleFailed += OnSubtitleFailed;
    }

    private static string Text(string key) => (string)Application.Current.FindResource(key);

    private void OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Open, Close and backend replacement publish a cleared subtitle, including same-URI opens.
        if (e.PropertyName == nameof(MediaFoundation.Controls.MediaPlayer.Subtitle) && _control.Player.Subtitle == null)
            _mediaVersion++;
        if (_observedSource != _control.Player.Source)
        {
            _observedSource = _control.Player.Source;
            _sourceVersion++;
            _embedded.Items.Clear();
            _embedded.Items.Add(_readEmbedded);
        }
    }

    private void Refresh()
    {
        bool active = _control.Player.Subtitle != null;
        _readEmbedded.IsEnabled = !_readingEmbedded && _control.Player.Source is { IsFile: true };
        _readEmbedded.Header = Text(_readingEmbedded ? "SubtitleReadingEmbedded" : "SubtitleReadEmbedded");
        _load.IsEnabled = !_loading && _control.Player.HasVideo && _control.Player.IsReady;
        _clear.IsEnabled = _earlier.IsEnabled = _later.IsEnabled = _reset.IsEnabled = active;
        _reset.Header = string.Format(Text("SubtitleDelay"), _control.Player.SubtitleDelay.TotalSeconds);
    }

    private async void OnReadEmbeddedAsync(object sender, RoutedEventArgs e)
    {
        if (_disposed || _readingEmbedded || _control.Player.Source is not { IsFile: true } source) return;
        _readingEmbedded = true;
        int version = _sourceVersion;
        Refresh();
        try
        {
            var tracks = await EmbeddedSubtitleReader.LoadAsync(source.LocalPath, _lifetime.Token);
            if (_disposed || version != _sourceVersion) return;
            _embedded.Items.Clear();
            _embedded.Items.Add(_readEmbedded);
            _embedded.Items.Add(new Separator());
            if (tracks.Count == 0)
                _embedded.Items.Add(new MenuItem { Header = Text("SubtitleNoEmbedded"), IsEnabled = false });
            foreach (var track in tracks)
            {
                string label = string.Format(Text("SubtitleTrack"), track.Id, track.Name ?? track.Language ?? track.Codec);
                var item = new MenuItem
                {
                    Header = track.Document == null ? string.Format(Text("SubtitleUnsupportedTrack"), label, track.UnsupportedReason) : label,
                    IsEnabled = track.Document != null,
                    ToolTip = track.Codec
                };
                item.Click += (_, _) =>
                {
                    if (version != _sourceVersion) return;
                    _control.Player.SubtitleDelay = TimeSpan.Zero;
                    _control.Player.Subtitle = track.Document;
                };
                _embedded.Items.Add(item);
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        {
            if (!_disposed && version == _sourceVersion) ShowError(error);
        }
        finally
        {
            _readingEmbedded = false;
            if (!_disposed) Refresh();
        }
    }

    private async void OnLoadAsync(object sender, RoutedEventArgs e)
    {
        if (_loading || _disposed) return;
        var dialog = new OpenFileDialog { Filter = Text("SubtitleFilter"), Title = Text("SubtitleLoad") };
        var owner = Window.GetWindow(_control);
        if (dialog.ShowDialog(owner) != true) return;
        _loading = true;
        int version = _mediaVersion;
        try
        {
            var document = await SubtitleReader.LoadAsync(dialog.FileName, _lifetime.Token);
            if (!_disposed && version == _mediaVersion)
            {
                _control.Player.SubtitleDelay = TimeSpan.Zero;
                _control.Player.Subtitle = document;
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException or NotSupportedException or DecoderFallbackException or ArgumentException or System.Security.SecurityException)
        {
            if (!_disposed && version == _mediaVersion) ShowError(error);
        }
        finally { _loading = false; }
    }

    private void OnSubtitleFailed(object? sender, PlaybackFailedEventArgs e) => ShowError(e.Error);

    private void ShowError(Exception error)
    {
        PlaybackDiagnostics.Write(error, _control.Player.Source);
        var owner = Window.GetWindow(_control);
        if (owner != null)
            MessageBox.Show(owner, string.Format(Text("SubtitleFailure"), error.Message), Text("SubtitleMenu"), MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _menu.IsOpen = false;
        if (ReferenceEquals(_control.ContextMenu, _menu)) _control.ContextMenu = null;
        _control.Player.PropertyChanged -= OnChanged;
        _control.Player.SubtitleFailed -= OnSubtitleFailed;
    }
}
