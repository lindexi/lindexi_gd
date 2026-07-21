using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JacaikecejihaiweHeqajearhi;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly TimeSpan RenderingSampleInterval = TimeSpan.FromSeconds(1);

    private readonly MainWindowViewModel _viewModel = new();
    private readonly OneWayAlphaWindowController _alphaController;
    private readonly Stopwatch _renderingStopwatch = Stopwatch.StartNew();

    private TimeSpan _lastRenderingSampleTime;
    private long _renderedFrameCount;
    private long _lastRenderingSampleFrameCount;
    private bool _closed;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;
        _alphaController = new OneWayAlphaWindowController(this);

        CompositionTarget.Rendering += OnCompositionTargetRendering;
        Closed += OnWindowClosed;
    }

    private async void UpgradeToTransparentButtonClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsUpgradeEnabled)
        {
            return;
        }

        TransitionShield.Visibility = Visibility.Visible;
        _viewModel.MarkUpgrading();

        try
        {
            await _alphaController.UpgradeToTransparentAsync(PrepareTransparentContent);
            _viewModel.MarkAlphaModeApplied();

            await Dispatcher.InvokeAsync(
                static () => { },
                DispatcherPriority.ContextIdle);

            if (!_closed)
            {
                TransitionShield.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException) when (_closed)
        {
        }
        catch (Win32Exception exception)
        {
            HandleUpgradeFailure(exception.Message);
        }
        catch (NotSupportedException exception)
        {
            HandleUpgradeFailure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            HandleUpgradeFailure(exception.Message);
        }
    }

    private void PrepareTransparentContent()
    {
        Background = Brushes.Transparent;
        RootSurface.Background = Brushes.Transparent;
    }

    private void HandleUpgradeFailure(string errorMessage)
    {
        _viewModel.MarkFaulted();

        Background = (Brush)FindResource("WindowOpaqueBackgroundBrush");
        RootSurface.Background = (Brush)FindResource("WindowOpaqueBackgroundBrush");
        TransitionShield.Visibility = Visibility.Collapsed;

        MessageBox.Show(
            this,
            $"无法切换到透明模式。\n\n{errorMessage}",
            "透明模式升级失败",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        _renderedFrameCount++;

        TimeSpan currentTime = _renderingStopwatch.Elapsed;
        TimeSpan sampleDuration = currentTime - _lastRenderingSampleTime;
        if (sampleDuration < RenderingSampleInterval)
        {
            return;
        }

        long sampledFrameCount = _renderedFrameCount - _lastRenderingSampleFrameCount;
        double framesPerSecond = sampledFrameCount / sampleDuration.TotalSeconds;
        double averageFrameTimeMilliseconds = framesPerSecond > 0
            ? TimeSpan.FromSeconds(1 / framesPerSecond).TotalMilliseconds
            : 0;

        _viewModel.UpdateRenderingMetrics(
            framesPerSecond,
            averageFrameTimeMilliseconds,
            _renderedFrameCount);

        _lastRenderingSampleTime = currentTime;
        _lastRenderingSampleFrameCount = _renderedFrameCount;
    }

    private void TitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizedState();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void MinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButtonClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizedState();
    }

    private void CloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximizedState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _closed = true;

        CompositionTarget.Rendering -= OnCompositionTargetRendering;
        Closed -= OnWindowClosed;
        _alphaController.Dispose();
    }
}