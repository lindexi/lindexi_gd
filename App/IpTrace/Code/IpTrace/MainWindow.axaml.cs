using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace IpTrace;

public partial class MainWindow : Window
{
    private static readonly IBrush ReadyBrush = Brush.Parse("#60A5FA");
    private static readonly IBrush RunningBrush = Brush.Parse("#D99A20");
    private static readonly IBrush SuccessBrush = Brush.Parse("#16A379");
    private static readonly IBrush ErrorBrush = Brush.Parse("#DC5252");

    private readonly NetworkDiagnosticService _diagnosticService = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _elapsedTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private CancellationTokenSource? _operationCancellation;

    public MainWindow()
    {
        InitializeComponent();
        _elapsedTimer.Tick += (_, _) => UpdateElapsed();
        Closed += (_, _) =>
        {
            _elapsedTimer.Stop();
            _operationCancellation?.Cancel();
            _diagnosticService.Dispose();
        };
    }

    private string ResourceText(string key) => (string)Resources[key]!;

    private void ModeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton button || IsOperationRunning())
        {
            return;
        }

        ToolTitleTextBlock.Text = button.Tag as string;
        ToolDescriptionTextBlock.Text = ResourceText(button == PingButton ? "PingDescription"
            : button == TcpPingButton ? "TcpDescription"
            : button == TraceRouteButton ? "TraceDescription" : "DnsDescription");
        PortPanel.IsVisible = button == TcpPingButton;
        if (EmptyStatePanel.IsVisible)
        {
            SummaryModeTextBlock.Text = ToolTitleTextBlock.Text;
        }
    }

    private async void RunButton_OnClick(object? sender, RoutedEventArgs e) => await RunSelectedOperationAsync();

    private async void TargetTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !IsOperationRunning())
        {
            e.Handled = true;
            await RunSelectedOperationAsync();
        }
    }

    private async Task RunSelectedOperationAsync()
    {
        if (IsOperationRunning())
        {
            return;
        }

        string target;
        try
        {
            target = NetworkDiagnosticService.NormalizeTarget(TargetTextBox.Text ?? string.Empty);
        }
        catch (ArgumentException exception)
        {
            ShowError(exception.Message);
            TargetTextBox.Focus();
            return;
        }

        TargetTextBox.Text = target;
        SummaryTargetTextBlock.Text = target;
        SummaryModeTextBlock.Text = ToolTitleTextBlock.Text;
        OutputTextBox.Text = string.Empty;
        ShowOutput();
        using var cancellation = new CancellationTokenSource();
        _operationCancellation = cancellation;
        SetOperationButtonsEnabled(false);
        SetStatus(ResourceText("RunningText"), RunningBrush);
        _stopwatch.Restart();
        UpdateElapsed();
        _elapsedTimer.Start();

        try
        {
            if (TraceRouteButton.IsChecked == true)
            {
                await _diagnosticService.TraceRouteAsync(target, AppendOutputLineAsync, cancellation.Token);
            }
            else
            {
                OutputTextBox.Text = PingButton.IsChecked == true
                    ? await _diagnosticService.PingAsync(target, cancellation.Token)
                    : TcpPingButton.IsChecked == true
                        ? await _diagnosticService.TcpPingAsync(target, decimal.ToInt32(PortNumericUpDown.Value ?? 443), cancellation.Token)
                        : await _diagnosticService.QueryAsync(target, cancellation.Token);
            }

            cancellation.Token.ThrowIfCancellationRequested();
            SetStatus(ResourceText("CompletedText"), SuccessBrush);
        }
        catch (OperationCanceledException)
        {
            AppendOutputLine(ResourceText("CancelledText"));
            SetStatus(ResourceText("CancelledText"), ReadyBrush);
        }
        catch (Exception exception) when (exception is SocketException or InvalidOperationException or ArgumentException or PingException)
        {
            ShowError(exception.Message);
        }
        finally
        {
            _stopwatch.Stop();
            _elapsedTimer.Stop();
            UpdateElapsed();
            _operationCancellation = null;
            SetOperationButtonsEnabled(true);
        }
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _operationCancellation?.Cancel();
        CancelButton.IsEnabled = false;
        SetStatus(ResourceText("CancellingText"), RunningBrush);
    }

    private void ClearButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (IsOperationRunning())
        {
            return;
        }

        OutputTextBox.Text = string.Empty;
        OutputTextBox.IsVisible = false;
        EmptyStatePanel.IsVisible = true;
        SummaryTargetTextBlock.Text = ResourceText("NoTargetText");
        SummaryModeTextBlock.Text = ToolTitleTextBlock.Text;
        ElapsedTextBlock.Text = "—";
        SetStatus(ResourceText("ReadyText"), ReadyBrush);
    }

    private bool IsOperationRunning() => _operationCancellation is not null;

    private void SetOperationButtonsEnabled(bool enabled)
    {
        QueryButton.IsEnabled = enabled;
        PingButton.IsEnabled = enabled;
        TcpPingButton.IsEnabled = enabled;
        TraceRouteButton.IsEnabled = enabled;
        RunButton.IsEnabled = enabled;
        TargetTextBox.IsEnabled = enabled;
        PortNumericUpDown.IsEnabled = enabled;
        ClearButton.IsEnabled = enabled;
        CancelButton.IsEnabled = !enabled;
        CancelButton.IsVisible = !enabled;
        OperationProgressBar.IsVisible = !enabled;
    }

    private void UpdateElapsed() => ElapsedTextBlock.Text = string.Format(
        CultureInfo.CurrentCulture, ResourceText("ElapsedFormat"), _stopwatch.Elapsed.TotalSeconds);

    private Task AppendOutputLineAsync(string line) => Dispatcher.UIThread.InvokeAsync(() => AppendOutputLine(line)).GetTask();

    private void AppendOutputLine(string line)
    {
        OutputTextBox.Text = string.IsNullOrEmpty(OutputTextBox.Text)
            ? line
            : $"{OutputTextBox.Text}{Environment.NewLine}{line}";
        OutputTextBox.CaretIndex = OutputTextBox.Text.Length;
    }

    private void ShowOutput()
    {
        EmptyStatePanel.IsVisible = false;
        OutputTextBox.IsVisible = true;
    }

    private void ShowError(string message)
    {
        ShowOutput();
        AppendOutputLine(message);
        SetStatus(ResourceText("FailedText"), ErrorBrush);
    }

    private void SetStatus(string status, IBrush brush)
    {
        StatusTextBlock.Text = status;
        StatusIndicator.Fill = brush;
    }
}
