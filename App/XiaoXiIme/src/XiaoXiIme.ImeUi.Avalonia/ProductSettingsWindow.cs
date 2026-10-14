using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class ProductSettingsWindow : Window
{
    private readonly ProductUiController _owner;
    private readonly ComboBox _scheme = new() { ItemsSource = new[] { ProductResources.Text("Full"), ProductResources.Text("Double") }, SelectedIndex = 0 };
    private readonly CheckBox _visible = new() { Content = ProductResources.Text("Visible"), IsChecked = true };
    private readonly TextBlock _current = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel _editor = new() { Spacing = 12 };
    private readonly Button _apply;
    private readonly Button _ok;
    private ImeProductSettingsResponse? _baseline;
    private ImeStatusBarPosition? _position;
    private bool _updating, _busy, _available, _conflict;

    internal ProductSettingsWindow(ProductUiController owner)
    {
        _owner = owner;
        Title = ProductResources.Text("SettingsTitle");
        Width = 560; Height = 440;
        var root = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        root.Children.Add(_current);
        _editor.Children.Add(_scheme);
        _editor.Children.Add(new TextBlock { Text = ProductResources.Text("Hint"), TextWrapping = TextWrapping.Wrap });
        _editor.Children.Add(_visible);
        _editor.Children.Add(Button("Position", () => { _position = null; UpdateButtons(); }));
        root.Children.Add(_editor);
        root.Children.Add(_message);
        var buttons = new WrapPanel();
        buttons.Children.Add(Button("Defaults", () => { if (_busy) return; _scheme.SelectedIndex = 0; _visible.IsChecked = true; _position = null; UpdateButtons(); }));
        buttons.Children.Add(Button("Reload", async () => { if (_busy) return; _baseline = null; _conflict = false; await _owner.RefreshAsync(); }));
        buttons.Children.Add(Button("Cancel", () => Close()));
        _apply = Button("Apply", async () => await SubmitAsync(false));
        _ok = Button("OK", async () => await SubmitAsync(true));
        buttons.Children.Add(_apply); buttons.Children.Add(_ok);
        root.Children.Add(buttons);
        Content = root;
        _scheme.SelectionChanged += (_, _) => UpdateButtons();
        _visible.IsCheckedChanged += (_, _) => UpdateButtons();
        Closing += (_, e) => e.Cancel = _busy;
        KeyDown += (_, e) => { if (e.Key == Key.Escape && !_busy) { Close(); e.Handled = true; } };
        UpdateButtons();
    }

    private static Button Button(string key, Action action)
    {
        var button = new Button { Content = ProductResources.Text(key), Margin = new Thickness(0, 0, 8, 4) };
        button.Click += (_, _) => action();
        return button;
    }

    private ImeProductSettings Draft => (_baseline?.Settings ?? new()) with
    {
        InputScheme = _scheme.SelectedIndex == 1 ? ImeProductInputScheme.XiaoheDoublePinyin : ImeProductInputScheme.FullPinyin,
        StatusBarVisible = _visible.IsChecked == true,
        StatusBarPosition = _position
    };
    private bool Dirty => _baseline is not null && Draft != _baseline.Settings;
    internal object ValidationState => new { baseline = _baseline?.Settings, draft = Draft, dirty = Dirty, busy = _busy, conflict = _conflict };

    internal void Observe(ImeProductSettingsResponse state)
    {
        _current.Text = ProductResources.Format("Current", ProductResources.Scheme(state.CurrentInputScheme),
            ProductResources.Text(state.Settings.InputScheme == ImeProductInputScheme.FullPinyin ? "Full" : "Double"));
        if (state.InputSchemeSource is "explicitPath" or "explicitParameter" or "environment")
            _current.Text += "\n" + ProductResources.Format("Override", ProductResources.Text(state.InputSchemeSource));
        if (_baseline is not null && (_baseline.HostInstanceId != state.HostInstanceId || _baseline.Settings.Revision != state.Settings.Revision) && Dirty)
        {
            _conflict = true;
            Message("Conflict");
        }
        else if (_baseline is null || !Dirty) Load(state);
        UpdateButtons();
    }

    private void Load(ImeProductSettingsResponse state)
    {
        _updating = true;
        _baseline = state;
        _scheme.SelectedIndex = (int)state.Settings.InputScheme;
        _visible.IsChecked = state.Settings.StatusBarVisible;
        _position = state.Settings.StatusBarPosition;
        _conflict = false;
        _updating = false;
    }
    internal void Saved(ImeProductSettingsResponse state) { Load(state); Message(state.RequiresHostRestart ? "Pending" : "None"); UpdateButtons(); }
    internal void Message(string key) => _message.Text = ProductResources.Text(key);
    internal void SetAvailable(bool available) { _available = available; UpdateButtons(); }
    internal void SetBusy(bool busy) { _busy = busy; _editor.IsEnabled = !busy; UpdateButtons(); }
    private void UpdateButtons()
    {
        if (_updating) return;
        _apply.IsEnabled = !_busy && _available && !_conflict && Dirty && _baseline?.Error == ImeProductSettingsError.None;
        _ok.IsEnabled = !_busy && _available && !_conflict && _baseline?.Error == ImeProductSettingsError.None;
    }
    private async Task SubmitAsync(bool close)
    {
        if (_baseline is null || _busy) return;
        if (!Dirty) { if (close) Close(); return; }
        if (await _owner.SaveAsync(_baseline, Draft, this) && close) Close();
    }
}
