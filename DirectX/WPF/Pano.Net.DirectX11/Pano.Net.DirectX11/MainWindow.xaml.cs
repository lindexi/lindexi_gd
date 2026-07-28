using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;

namespace Pano.Net.DirectX11;

public partial class MainWindow : Window
{
    private readonly PanoramaCamera _camera = new();
    private readonly DirectX11D3DImageRenderer _renderer;
    private Point _previousPosition;
    private bool _isDragging;
    private bool _rendererFailed;

    public MainWindow()
    {
        InitializeComponent();

        _renderer = new DirectX11D3DImageRenderer(RenderImage);
        Loaded += OnLoaded;
        Closed += OnClosed;
        RenderImage.SizeChanged += OnRenderImageSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ExecuteRendererOperation(() =>
        {
            _renderer.Initialize();
            UpdateLayout();
            ResizeRenderer();
            UpdateStatus(_renderer.Status);
        });
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _renderer.Dispose();
    }

    private void OnRenderImageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ExecuteRendererOperation(ResizeRenderer);
    }

    private void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "全景图像|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ExecuteRendererOperation(() =>
        {
            _renderer.LoadPanorama(dialog.FileName);
            Render();
        });
    }

    private void CopyStatus_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(StatusText.Text))
        {
            Clipboard.SetText(StatusText.Text);
        }
    }

    private void RenderImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _previousPosition = e.GetPosition(RenderImage);
        _isDragging = true;
        RenderImage.CaptureMouse();
    }

    private void RenderImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        RenderImage.ReleaseMouseCapture();
    }

    private void RenderImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        Point position = e.GetPosition(RenderImage);
        _camera.Rotate(position - _previousPosition, RenderImage.RenderSize);
        _previousPosition = position;
        Render();
    }

    private void RenderImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _camera.Zoom(Math.Pow(1.1, e.Delta / 120.0));
        Render();
    }

    private void RenderImage_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
    {
        e.ManipulationContainer = RenderImage;
        e.Mode = ManipulationModes.Translate | ManipulationModes.Scale;
        e.Handled = true;
    }

    private void RenderImage_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
        _camera.Rotate(e.DeltaManipulation.Translation, RenderImage.RenderSize);
        _camera.Zoom(e.DeltaManipulation.Scale.X);
        Render();
        e.Handled = true;
    }

    private void ResizeRenderer()
    {
        if (_rendererFailed)
        {
            return;
        }

        int width = Math.Max(1, (int)Math.Ceiling(RenderImage.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(RenderImage.ActualHeight));
        _renderer.Resize(width, height);
        Render();
    }

    private void Render()
    {
        if (_rendererFailed)
        {
            return;
        }

        _renderer.Render(_camera.Yaw, _camera.Pitch, _camera.FieldOfView);
        UpdateStatus(_renderer.Status);
    }

    private void ExecuteRendererOperation(Action operation)
    {
        try
        {
            operation();
        }
        catch (Exception exception)
        {
            _rendererFailed = true;
            UpdateStatus($"DirectX 11 渲染错误：{exception.Message}");
        }
    }

    private void UpdateStatus(string status)
    {
        StatusText.Text = $"{status}  方位 {_camera.Yaw:0.0}°  仰角 {_camera.Pitch:0.0}°  FOV {_camera.FieldOfView:0.0}°";
    }
}
