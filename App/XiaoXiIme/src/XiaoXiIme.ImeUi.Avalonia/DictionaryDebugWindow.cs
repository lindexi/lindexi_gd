using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class DictionaryDebugWindow : Window
{
    private readonly TextBox output = new()
    {
        Name = "DebugOutput",
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
    };
    private readonly TextBox log = new()
    {
        IsReadOnly = true,
        AcceptsReturn = true,
        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
    };
    private readonly CandidateWindow candidates = new();
    private readonly Button previousPage = new() { Name = "DictionaryDebugPreviousPage", Content = DebugResources.PreviousPage, Focusable = false, IsEnabled = false };
    private readonly Button nextPage = new() { Name = "DictionaryDebugNextPage", Content = DebugResources.NextPage, Focusable = false, IsEnabled = false };
    private DictionaryDebugSession? session;
    private readonly SemaphoreSlim keyQueue = new(1, 1);
    private object? processingValidation;
    private string keyPhase = "idle";
    internal string KeyPhase => Volatile.Read(ref keyPhase);
    private bool closed;
    private bool settingsActive;
    private readonly DictionaryDebugSettings settings;
    private readonly string? packagePath;

    internal DictionaryDebugWindow(string? settingsPath = null, bool openSettings = false, string? packagePath = null)
    {
        this.packagePath = packagePath;
        Title = DebugResources.WindowTitle;
        Width = 800;
        Height = 640;
        MinHeight = 360;
        var layout = new Grid
        {
            Margin = new Thickness(16),
            RowDefinitions = new RowDefinitions("Auto,3*,Auto,2*"),
        };
        var outputLabel = new TextBlock { Text = DebugResources.OutputLabel, Margin = new Thickness(0, 0, 0, 6) };
        var logLabel = new TextBlock { Text = DebugResources.LogLabel, Margin = new Thickness(0, 0, 0, 6) };
        var splitter = new GridSplitter
        {
            Height = 10,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
            ResizeDirection = GridResizeDirection.Rows,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
        };
        var logPanel = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        logPanel.Children.Add(logLabel);
        Grid.SetRow(log, 1);
        logPanel.Children.Add(log);
        Grid.SetRow(output, 1);
        Grid.SetRow(splitter, 2);
        Grid.SetRow(logPanel, 3);
        var toolbar = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
        var settingsButton = new Button { Name = "DictionaryDebugSettings", Content = ProductResources.Text("Settings") };
        global::Avalonia.Automation.AutomationProperties.SetAutomationId(settingsButton, settingsButton.Name);
        global::Avalonia.Automation.AutomationProperties.SetAutomationId(previousPage, previousPage.Name);
        global::Avalonia.Automation.AutomationProperties.SetAutomationId(nextPage, nextPage.Name);
        settingsButton.Click += OnSettingsClick;
        previousPage.Click += (_, _) => ChangePage(Key.PageUp);
        nextPage.Click += (_, _) => ChangePage(Key.PageDown);
        toolbar.Children.Add(settingsButton);
        toolbar.Children.Add(previousPage);
        toolbar.Children.Add(nextPage);
        toolbar.Children.Add(outputLabel);
        layout.Children.Add(toolbar);
        layout.Children.Add(output);
        layout.Children.Add(splitter);
        layout.Children.Add(logPanel);
        Content = layout;
        InputMethod.SetIsInputMethodEnabled(this, false);
        InputMethod.SetIsInputMethodEnabled(output, false);
        InputMethod.SetIsInputMethodEnabled(log, false);
        log.GotFocus += (_, _) =>
        {
            session?.CancelComposition();
            UpdatePagingControls();
            candidates.Hide();
        };
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, (_, args) => args.Handled = true, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        if (openSettings) Opened += OnSettingsClick;
        Deactivated += (_, _) =>
        {
            if (settingsActive) return;
            session?.CancelComposition();
            UpdatePagingControls();
            candidates.Hide();
        };
        PositionChanged += (_, _) => PositionCandidates();
        try { settings = new(this, settingsPath, packagePath); }
        catch (IOException) { candidates.Close(); throw; }
        catch (UnauthorizedAccessException) { candidates.Close(); throw; }
        Closing += async (_, args) =>
        {
            settings.PrepareOwnerClose();
            var pending = settings.PendingSave;
            if (pending.IsCompleted) return;
            args.Cancel = true;
            await pending;
            if (!closed) Close();
        };
        Closed += (_, _) =>
        {
            closed = true;
            candidates.Close();
            settings.Dispose();
        };
    }

    private void OnSettingsClick(object? sender, EventArgs args)
    {
        if (processingValidation is null) settings.Open();
    }

    internal void OpenFromEntry(bool openSettings)
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        if (openSettings) settings.Open();
    }

    private async void OnOpened(object? sender, EventArgs args)
    {
        var packagePath = DictionaryPackageLocations.ResolvePackageDirectory(this.packagePath, inputScheme: settings.InitialScheme);
        Title = DebugResources.LoadingTitle;
        AppendLog(string.Format(DebugResources.PackagePath, packagePath));
        AppendLog(DebugResources.LoadingTitle);
        try
        {
            var initialRuleMask = settings.InitialRuleMask;
            var prepared = await Task.Run(() =>
            {
                var dictionary = DictionaryPackageLoader.Load(packagePath);
                var session = new DictionaryDebugSession(dictionary).PrepareFuzzyRulesChange(initialRuleMask);
                return (Session: session, Scheme: dictionary.InputScheme);
            });
            if (closed)
                return;
            session = prepared.Session;
            settings.SetScheme(prepared.Scheme);
            UpdatePagingControls();
            Title = DebugResources.WindowTitle;
            AppendLog(DebugResources.LoadSucceeded);
            if (!log.IsKeyboardFocusWithin)
                output.Focus();
        }
        catch (DictionaryPackageException exception)
        {
            if (!closed)
            {
                Title = DebugResources.LoadFailedTitle;
                AppendLog(exception.ToString());
                AppendLog(DebugResources.PackageHint);
            }
        }
    }

    private async void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (settingsActive || !output.IsKeyboardFocusWithin || session is null && processingValidation is null)
            return;
        if ((args.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
            return;

        args.Handled = true;
        var key = args.Key;
        var modifiers = args.KeyModifiers;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        void Phase(string phase)
        {
            Volatile.Write(ref keyPhase, $"{key}:{phase}");
            SettingsApplyTrace.Mark(KeyPhase, started);
        }
        Phase("queue-wait");
        await keyQueue.WaitAsync();
        Phase("queue-acquired");
        bool handled;
        try
        {
            if (closed || settingsActive || session is null) return;
            var active = session;
            processingValidation = ValidationState;
            session = null;
            try
            {
                handled = await Task.Run(() =>
                {
                    Phase("worker-start");
                    var result = active.ProcessKey(key, modifiers);
                    Phase("worker-end");
                    return result;
                });
                Phase("dispatcher-resumed");
            }
            finally
            {
                session = active;
                processingValidation = null;
            }
            if (closed) return;
        }
        finally
        {
            keyQueue.Release();
        }
        Phase("render-start");
        UpdatePagingControls();
        Phase("paging-updated");
        AppendLog($"Key={args.Key}; Modifiers={args.KeyModifiers}; Handled={handled}; Composition={session.Candidates.CompositionText}; Page={session.Candidates.CurrentPage}; Candidates={session.Candidates.Candidates.Count}");
        if (!handled)
            return;

        Phase("log-updated");
        args.Handled = true;
        output.Text = session.CommittedText;
        output.CaretIndex = session.CommittedText.Length;
        candidates.DataContext = session.Candidates;
        Phase("candidates-updated");
        if (session.Candidates.IsVisible)
        {
            PositionCandidates();
            if (!candidates.IsVisible)
                candidates.Show(this);
        }
        else
        {
            candidates.Hide();
        }
        Phase("completed");
    }

    internal object ValidationState => processingValidation ?? new { reading = session?.Candidates.CompositionText, caret = session?.CompositionCaret, output = session?.CommittedText, candidates = session?.Candidates.Candidates, page = session?.Candidates.CurrentPage, totalPages = session?.Candidates.TotalPages, engine = session?.ValidationState, rulemask = settings.AppliedRuleMask, settingsActive, loaded = session is not null, focused = output.IsKeyboardFocusWithin, log = log.Text };
    private void UpdatePagingControls()
    {
        var state = session?.Candidates;
        var available = !settingsActive && state?.IsVisible == true;
        previousPage.IsEnabled = available && state!.CurrentPage > 1;
        nextPage.IsEnabled = available && state!.CurrentPage < state.TotalPages;
    }

    internal int? ValidationPage => session?.Candidates.CurrentPage;

    private void ChangePage(Key key)
    {
        if (session is null || settingsActive || !(key == Key.PageUp ? previousPage.IsEnabled : nextPage.IsEnabled))
            return;

        session.ProcessKey(key);
        UpdatePagingControls();
        candidates.DataContext = session.Candidates;
        output.Focus();
        if (session.Candidates.IsVisible && IsVisible)
        {
            PositionCandidates();
            if (!candidates.IsVisible) candidates.Show(this);
        }
    }

    internal void SuspendSettings() { settingsActive = true; UpdatePagingControls(); candidates.Hide(); }
    internal void ResumeSettings()
    {
        if (closed) return;
        settingsActive = false;
        UpdatePagingControls();
        output.Focus();
        if (session?.Candidates.IsVisible == true)
        {
            candidates.DataContext = session.Candidates;
            PositionCandidates();
            candidates.Show(this);
        }
    }
    internal DictionaryDebugSession CaptureSettingsSession() =>
        settingsActive ? session ?? throw new InvalidOperationException() : throw new InvalidOperationException();
    internal DictionaryDebugSession PrepareDictionaryChange(IImeDictionary dictionary) =>
        (session ?? throw new InvalidOperationException()).PrepareDictionaryChange(dictionary);
    internal DictionaryDebugSession PrepareFuzzyRulesChange(int mask) =>
        (session ?? throw new InvalidOperationException()).PrepareFuzzyRulesChange(mask);
    internal void PublishSession(DictionaryDebugSession prepared)
    {
        session = prepared;
        UpdatePagingControls();
    }

    private void AppendLog(string message)
    {
        log.Text += $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
        if (!log.IsKeyboardFocusWithin)
            log.CaretIndex = log.Text.Length;
    }

    private void PositionCandidates()
    {
        candidates.Position = output.PointToScreen(new Point(0, output.Bounds.Height));
    }
}
