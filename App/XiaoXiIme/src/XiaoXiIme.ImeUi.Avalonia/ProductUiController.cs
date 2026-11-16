using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class ProductUiController : IDisposable
{
    private readonly XiaoXiImeIpcClient _client;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly StatusBarWindow _bar;
    private readonly TrayIcon _tray;
    private ImeProductSettingsResponse? _state;
    private ImeHostStatus? _host;
    private ProductSettingsWindow? _settings;
    private Window? _about;
    private TextBlock? _aboutText;
    private bool _refreshing, _saving, _disposed, _connected, _temporaryVisible;
    private ImeSaveProductSettingsRequest? _uncertain;
    private ProductSettingsWindow? _uncertainWindow;

    internal ProductUiController(XiaoXiImeIpcOptions options)
    {
        _client = new(options);
        _bar = new(this);
        var menu = new NativeMenu();
        AddMenu(menu, "Visible", () => ChangeVisibilityAsync(true));
        AddMenu(menu, "Hide", () => ChangeVisibilityAsync(false));
        AddMenu(menu, "Settings", () => { OpenSettings(); return Task.CompletedTask; });
        AddMenu(menu, "About", () => { OpenAbout(); return Task.CompletedTask; });
        AddMenu(menu, "Exit", () => { Exit(); return Task.CompletedTask; });
        using var bitmap = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(32, 32));
        using (var drawing = bitmap.CreateDrawingContext()) drawing.FillRectangle(Brushes.RoyalBlue, new Rect(0, 0, 32, 32));
        _tray = new TrayIcon { Icon = new WindowIcon(bitmap), ToolTipText = ProductResources.Text("Title"), Menu = menu, IsVisible = true };
        _tray.Clicked += OnTrayClicked;
        _timer.Tick += OnTick;
        _timer.Start();
        _bar.ApplyPosition(null);
        _bar.Show();
    }

    private static void AddMenu(NativeMenu menu, string key, Func<Task> action)
    {
        var item = new NativeMenuItem(ProductResources.Text(key));
        item.Click += async (_, _) => await action();
        menu.Items.Add(item);
    }
    private async void OnTrayClicked(object? sender, EventArgs e) => await ChangeVisibilityAsync(true);
    private async void OnTick(object? sender, EventArgs e) => await RefreshAsync();

    internal async Task RefreshAsync()
    {
        if (_refreshing || _saving || _disposed) return;
        _refreshing = true;
        try
        {
            var state = await _client.GetProductSettingsAsync();
            var host = await _client.GetHostStatusAsync();
            if (_disposed || _saving) return;
            _connected = true;
            if (_uncertain is { } pending)
            {
                if (state.HostInstanceId != pending.HostInstanceId)
                {
                    _uncertain = null;
                    _uncertainWindow?.SetBusy(false);
                    _uncertainWindow?.Message("HostChanged");
                    _uncertainWindow = null;
                }
                else if (state.LastOperationId == pending.OperationId)
                {
                    _uncertain = null;
                    _uncertainWindow?.Saved(state);
                    _uncertainWindow?.SetBusy(false);
                    _uncertainWindow = null;
                }
                else
                {
                    // A read can precede the in-flight commit. Only an idempotent replay resolves it.
                    var confirmation = await _client.SaveProductSettingsAsync(pending);
                    state = await _client.GetProductSettingsAsync();
                    if (_disposed) return;
                    _uncertain = null;
                    if (confirmation.Error == ImeProductSettingsError.None &&
                        confirmation.HostInstanceId == state.HostInstanceId && confirmation.Settings.Revision == state.Settings.Revision)
                        _uncertainWindow?.Saved(state);
                    else _uncertainWindow?.Message(confirmation.Error == ImeProductSettingsError.None ? "Conflict" : confirmation.Error.ToString());
                    _uncertainWindow?.SetBusy(false);
                    _uncertainWindow = null;
                }
            }
            Accept(state, host);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            if (_disposed) return;
            _connected = false;
            _settings?.Message(_uncertain is null ? "Offline" : "Uncertain");
            UpdateDisplay();
        }
        finally { _refreshing = false; if (!_disposed) _settings?.SetAvailable(_connected && _uncertain is null && !_saving); }
    }

    private void Accept(ImeProductSettingsResponse state, ImeHostStatus? host)
    {
        if (_disposed) return;
        if (_state is { } previous && previous.HostInstanceId == state.HostInstanceId && previous.Settings.Revision > state.Settings.Revision) return;
        _state = state;
        _host = host;
        _settings?.Observe(state);
        _bar.ApplyPosition(state.Settings.StatusBarPosition);
        if (state.Settings.StatusBarVisible || _temporaryVisible || _tray.NativeMenuExporter is null) _bar.Show(); else _bar.Hide();
        if (!state.Settings.StatusBarVisible && _tray.NativeMenuExporter is null)
            _settings?.Message("TrayUnavailable");
        UpdateDisplay();
    }

    private string Summary => !_connected ? ProductResources.Text("Offline") :
        _host is { IsUsingFallbackDictionary: true } || !string.IsNullOrEmpty(_host?.DictionaryLoadError) ? ProductResources.Text("Fallback") :
        !string.IsNullOrEmpty(_host?.UserDictionaryError) ? ProductResources.Text("UserError") :
        _state?.RequiresHostRestart == true ? ProductResources.Text("Pending") : ProductResources.Text("Ready");

    private void UpdateDisplay()
    {
        _bar.Status.Text = ProductResources.Scheme(_connected ? _state?.CurrentInputScheme : null) + " · " + Summary;
        if (_aboutText is not null) _aboutText.Text = Diagnostics();
    }

    private string Diagnostics()
    {
        var version = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? ProductResources.Text("UnknownVersion");
        return ProductResources.Format("DiagnosticSummary", ProductResources.Format("Version", version),
            ProductResources.Scheme(_connected ? _state?.CurrentInputScheme : null) + " · " + Summary,
            ProductResources.Text(!string.IsNullOrEmpty(_host?.DictionaryLoadError) ? "Yes" : "No"),
            ProductResources.Text(!string.IsNullOrEmpty(_host?.UserDictionaryError) ? "Yes" : "No"),
            ProductResources.Text((_state?.LoadError ?? ImeProductSettingsError.None).ToString()));
    }

    internal void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
        else Dispose();
    }

    internal void OpenSettings()
    {
        if (_disposed) return;
        if (_settings is null)
        {
            _settings = new(this);
            _settings.Closed += (_, _) => _settings = null;
            if (_state is not null) _settings.Observe(_state);
            _settings.SetAvailable(_connected && _uncertain is null);
        }
        _settings.Show();
        _settings.Activate();
    }

    internal void OpenAbout()
    {
        if (_disposed) return;
        if (_about is null)
        {
            _about = new Window { Title = ProductResources.Text("About"), Width = 460, Height = 340 };
            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = ProductResources.Text("Title"), FontSize = 22 });
            panel.Children.Add(new TextBlock { Text = ProductResources.Format("Attribution", ImeAboutState.SeWzc.SourceName) });
            panel.Children.Add(new TextBlock { Text = ProductResources.Format("Version", typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? ProductResources.Text("UnknownVersion")), TextWrapping = TextWrapping.Wrap });
            _aboutText = new TextBlock { Text = Diagnostics(), TextWrapping = TextWrapping.Wrap };
            panel.Children.Add(new Expander { Header = ProductResources.Text("Diagnostics"), Content = _aboutText });
            var copy = new Button { Content = ProductResources.Text("Copy") };
            copy.Click += async (_, _) => { if (_about?.Clipboard is { } clipboard) await clipboard.SetTextAsync(Diagnostics()); };
            panel.Children.Add(copy);
            _about.Content = panel;
            _about.Closed += (_, _) => { _about = null; _aboutText = null; };
        }
        _about.Show();
        _about.Activate();
    }

    internal async Task<bool> SaveAsync(ImeProductSettingsResponse baseline, ImeProductSettings draft, ProductSettingsWindow? submittingWindow = null)
    {
        if (_disposed || _saving || !_connected || _uncertain is not null) return false;
        var request = new ImeSaveProductSettingsRequest(baseline.HostInstanceId, Guid.NewGuid(), baseline.Settings.Revision, draft);
        _saving = true;
        _settings?.SetBusy(true);
        _settings?.Message("Saving");
        try
        {
            var response = await _client.SaveProductSettingsAsync(request);
            if (_disposed) return false;
            if (response.Error != ImeProductSettingsError.None) { _settings?.Message(response.Error.ToString()); return false; }
            _temporaryVisible = false;
            var latest = await _client.GetProductSettingsAsync();
            if (_disposed) return false;
            if (latest.HostInstanceId == response.HostInstanceId && latest.Settings.Revision == response.Settings.Revision)
                submittingWindow?.Saved(latest);
            else
            {
                submittingWindow?.Message("Conflict");
                Accept(latest, _host);
                return false;
            }
            Accept(latest, _host);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            if (_disposed) return false;
            _uncertain = request;
            _uncertainWindow = submittingWindow;
            _settings?.Message("Uncertain");
            return false;
        }
        finally { _saving = false; if (!_disposed) { _settings?.SetBusy(_uncertain is not null); _settings?.SetAvailable(_connected && _uncertain is null); } }
    }

    internal async Task ChangeVisibilityAsync(bool visible)
    {
        if (_disposed) return;
        if (visible) { _temporaryVisible = true; _bar.Show(); _bar.Status.Text = ProductResources.Text("Temporary"); }
        if (_state is { } state) await SaveAsync(state, state.Settings with { StatusBarVisible = visible });
    }

    internal async Task SavePositionAsync()
    {
        if (_state is { } state)
        {
            var saved = await SaveAsync(state, state.Settings with { StatusBarPosition = new(_bar.Position.X, _bar.Position.Y) });
            if (!saved && !_disposed) _bar.ApplyPosition(state.Settings.StatusBarPosition, force: true);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        _tray.Clicked -= OnTrayClicked;
        _tray.Dispose();
        _settings?.SetBusy(false);
        _settings?.Close();
        _about?.Close();
        _bar.Close();
        _client.Dispose();
    }
}
