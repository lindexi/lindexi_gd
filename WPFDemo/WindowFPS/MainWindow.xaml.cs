using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT.Interop;

namespace WindowFPS;

public partial class MainWindow : Window
{
    private static readonly Brush ActiveDot = Brushes.ForestGreen;
    private static readonly Brush IdleDot = Brushes.SlateGray;
    private readonly DispatcherTimer _timer;
    private IDirect3DDevice? _device;
    private WindowCapture? _capture;
    private bool _closed;

    public MainWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += OnUpdate;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) && !SystemParameters.HighContrast &&
            PresentationSource.FromVisual(this) is HwndSource source &&
            NativeMethods.TryEnableLightMica(source.Handle))
        {
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            Background = Brushes.Transparent;
        }
    }

    private async void OnPickWindow(object sender, RoutedEventArgs e)
    {
        if (!GraphicsCaptureSession.IsSupported())
        {
            ShowMessage("CaptureUnavailable");
            return;
        }

        PickButton.IsEnabled = false;
        try
        {
            var picker = new GraphicsCapturePicker();
            InitializeWithWindow.Initialize(picker, new WindowInteropHelper(this).Handle);
            var item = await picker.PickSingleItemAsync();
            if (_closed || item is null)
            {
                return;
            }

            var title = item.DisplayName;
            _device ??= NativeMethods.CreateCaptureDevice();
            var capture = new WindowCapture(item, _device);
            var previousCapture = _capture;
            _capture = capture;
            previousCapture?.Dispose();

            TargetTitle.Text = string.IsNullOrWhiteSpace(title) ? Text("UnnamedWindow") : title;
            TargetTitle.ToolTip = TargetTitle.Text;
            EmptyIcon.Visibility = Visibility.Collapsed;
            TargetIcon.Visibility = Visibility.Visible;
            PickButton.Content = Text("ChangeWindow");
            _timer.Start();
            UpdateReading();
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException or InvalidOperationException)
        {
            Trace.TraceError("Could not select or capture a window: {0}", ex);
            if (!_closed)
            {
                ShowMessage("CaptureFailed");
            }
        }
        finally
        {
            if (!_closed)
            {
                PickButton.IsEnabled = true;
            }
        }
    }

    private void OnUpdate(object? sender, EventArgs e) => UpdateReading();

    private void UpdateReading()
    {
        if (_capture is not { } capture)
        {
            return;
        }

        var (stopped, fps) = capture.GetReading();
        Reading.Text = fps?.ToString("0.0", CultureInfo.CurrentCulture) ?? Text("NoReading");
        Status.Visibility = Visibility.Visible;
        StatusText.Text = Text(stopped ? "CaptureStopped" : fps.HasValue ? "Capturing" : "NoFrames");
        StatusDot.Fill = fps.HasValue ? ActiveDot : IdleDot;

        if (stopped)
        {
            _timer.Stop();
            _capture = null;
            capture.Dispose();
        }
    }

    private string Text(string key) => (string)FindResource(key);

    private void ShowMessage(string key) => MessageBox.Show(this, Text(key), Title, MessageBoxButton.OK, MessageBoxImage.Warning);

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        _timer.Stop();
        _timer.Tick -= OnUpdate;
        try
        {
            _capture?.Dispose();
        }
        finally
        {
            _device?.Dispose();
            base.OnClosed(e);
        }
    }
}
