using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using XiaoXiIme.Foundation;
using XiaoXiIme.Dictionary;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class ProductSettingsWindow : Window
{
    private readonly Func<Task> _refresh;
    private readonly Func<ImeProductSettingsResponse, ImeProductSettings, ProductSettingsWindow, Task<bool>> _save;
    private readonly ComboBox _scheme = new()
    {
        ItemsSource = PhoneticSchemeCatalog.All
            .Select(scheme => new ComboBoxItem { Name = "Scheme" + scheme.DisplayResourceKey, Content = ProductResources.Text(scheme.DisplayResourceKey), Tag = scheme, IsEnabled = scheme.ProductScheme.HasValue }).ToArray(),
        SelectedIndex = 0
    };
    private readonly CheckBox _visible = new() { Name = "StatusBarVisible", Content = ProductResources.Text("Visible"), IsChecked = true };
    private readonly TextBlock _current = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _message = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel _editor = new() { Spacing = 12 };
    private readonly Dictionary<int, CheckBox> _rules = [];
    private readonly Button _apply;
    private readonly Button _ok;
    private ImeProductSettingsResponse? _baseline;
    private ImeStatusBarPosition? _position;
    private bool _updating, _busy, _available, _conflict;

    internal ProductSettingsWindow(ProductUiController owner) : this(owner.RefreshAsync, owner.SaveAsync) { }

    internal ProductSettingsWindow(Func<Task> refresh,
        Func<ImeProductSettingsResponse, ImeProductSettings, ProductSettingsWindow, Task<bool>> save)
    {
        _refresh = refresh;
        _save = save;
        Title = ProductResources.Text("SettingsTitle");
        Width = 720; Height = 660; MinWidth = 620; MinHeight = 560;
        var root = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        root.Children.Add(_current);
        _editor.Children.Add(new TextBlock { Text = ProductResources.Text("SchemeGroup") });
        _editor.Children.Add(_scheme);
        _editor.Children.Add(new TextBlock { Text = ProductResources.Text("Hint"), TextWrapping = TextWrapping.Wrap });
        _editor.Children.Add(new TextBlock { Text = ProductResources.Text("UnavailableSchemes"), TextWrapping = TextWrapping.Wrap });
        _editor.Children.Add(new TextBlock { Text = ProductResources.Text("FuzzyUnavailable"), TextWrapping = TextWrapping.Wrap });
        var rules = new[] { "ChC", "ShS", "ZhZ", "AngAn", "EngEn", "InIng", "IanIang", "UanUang", "AiAn", "UnOng" };
        var fuzzy = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto") };
        for (var i = 0; i < rules.Length; i++)
        {
            var checkbox = new CheckBox { Name = "Fuzzy" + rules[i], Content = ProductResources.Text("Fuzzy" + rules[i]), IsChecked = false };
            var rule = Enum.Parse<XiaoXiIme.ImeCore.ImeFuzzyRule>(rules[i]);
            _rules.Add((int)rule, checkbox);
            checkbox.IsCheckedChanged += (_, _) => UpdateButtons();
            global::Avalonia.Automation.AutomationProperties.SetAutomationId(checkbox, checkbox.Name);
            Grid.SetColumn(checkbox, i % 3); Grid.SetRow(checkbox, i / 3);
            fuzzy.Children.Add(checkbox);
        }
        _editor.Children.Add(fuzzy);
        _editor.Children.Add(new TextBlock { Text = ProductResources.Text("StatusGroup") });
        _editor.Children.Add(_visible);
        _editor.Children.Add(Button("Position", () => { _position = null; UpdateButtons(); }));
        _editor.Children.Add(_message);
        var scroll = new ScrollViewer { Content = _editor, Margin = new Thickness(0, 16) };
        Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        var buttons = new WrapPanel();
        buttons.Children.Add(Button("Defaults", () => { if (_busy) return; _scheme.SelectedIndex = 0; _visible.IsChecked = true; _position = null; foreach (var rule in _rules.Values) rule.IsChecked = false; UpdateButtons(); }));
        buttons.Children.Add(Button("Reload", async () => { if (_busy) return; _baseline = null; _conflict = false; await _refresh(); }));
        buttons.Children.Add(Button("Cancel", () => Close()));
        _apply = Button("Apply", async () => await SubmitAsync(false));
        _ok = Button("OK", async () => await SubmitAsync(true));
        buttons.Children.Add(_apply); buttons.Children.Add(_ok);
        Grid.SetRow(buttons, 2); root.Children.Add(buttons);
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
        InputScheme = (_scheme.SelectedItem is ComboBoxItem { Tag: PhoneticSchemeDefinition { ProductScheme: { } productScheme } })
            ? productScheme : (_baseline?.Settings.InputScheme ?? ImeProductInputScheme.FullPinyin),
        StatusBarVisible = _visible.IsChecked == true,
        StatusBarPosition = _position,
        FuzzyRuleMask = _rules.Where(p => p.Value.IsChecked == true).Aggregate(0, (mask, p) => mask | (1 << p.Key))
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
        _scheme.SelectedItem = _scheme.Items.OfType<ComboBoxItem>()
            .Single(item => ((PhoneticSchemeDefinition)item.Tag!).ProductScheme == state.Settings.InputScheme);
        _visible.IsChecked = state.Settings.StatusBarVisible;
        _position = state.Settings.StatusBarPosition;
        foreach (var rule in _rules) rule.Value.IsChecked = (state.Settings.FuzzyRuleMask & (1 << rule.Key)) != 0;
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
        if (await _save(_baseline, Draft, this) && close) Close();
    }
}
