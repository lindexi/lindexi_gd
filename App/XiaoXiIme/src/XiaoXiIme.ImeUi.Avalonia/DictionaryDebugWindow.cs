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
    private DictionaryDebugSession? session;
    private bool closed;

    internal DictionaryDebugWindow()
    {
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
        layout.Children.Add(outputLabel);
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
            candidates.Hide();
        };
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, (_, args) => args.Handled = true, RoutingStrategies.Tunnel);
        Opened += OnOpened;
        Deactivated += (_, _) =>
        {
            session?.CancelComposition();
            candidates.Hide();
        };
        PositionChanged += (_, _) => PositionCandidates();
        Closed += (_, _) =>
        {
            closed = true;
            candidates.Close();
        };
    }

    private async void OnOpened(object? sender, EventArgs args)
    {
        var packagePath = DictionaryPackageLocations.ResolvePackageDirectory(
            Environment.GetEnvironmentVariable("XIAOXIIME_DEBUG_PACKAGE"));
        Title = DebugResources.LoadingTitle;
        AppendLog(string.Format(DebugResources.PackagePath, packagePath));
        AppendLog(DebugResources.LoadingTitle);
        try
        {
            var dictionary = await Task.Run(() => DictionaryPackageLoader.Load(packagePath));
            if (closed)
                return;
            session = new DictionaryDebugSession(dictionary);
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

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (!output.IsKeyboardFocusWithin || session is null)
            return;

        var handled = session.ProcessKey(args.Key, args.KeyModifiers);
        AppendLog($"Key={args.Key}; Modifiers={args.KeyModifiers}; Handled={handled}; Composition={session.Candidates.CompositionText}; Page={session.Candidates.CurrentPage}; Candidates={session.Candidates.Candidates.Count}");
        if (!handled)
            return;

        args.Handled = true;
        output.Text = session.CommittedText;
        output.CaretIndex = session.CommittedText.Length;
        candidates.DataContext = session.Candidates;
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
