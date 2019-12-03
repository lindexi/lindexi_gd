using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace BicehecarayHerekurwuqear
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Setup();
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await StartCaptureAsync();
        }

        private void Setup()
        {
            _canvasDevice = new CanvasDevice();

            var compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(
                Window.Current.Compositor,
                _canvasDevice);

            var compositor = Window.Current.Compositor;

            _surface = compositionGraphicsDevice.CreateDrawingSurface(
                new Size(1000, 600),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
            // 现在只有这个参数能在 Composition 使用

            var visual = compositor.CreateSpriteVisual();
            visual.RelativeSizeAdjustment = Vector2.One;
            var brush = compositor.CreateSurfaceBrush(_surface);
            brush.Stretch = CompositionStretch.Uniform;
            visual.Brush = brush;
            ElementCompositionPreview.SetElementChildVisual(this, visual);

            _compositionGraphicsDevice = compositionGraphicsDevice;
        }

        public async Task StartCaptureAsync()
        {
            // 让用户选择哪个应用
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            // 如果用户有选择一个应用那么这个属性不为空
            if (item != null)
            {
                StartCaptureInternal(item);
            }
        }

        private void StartCaptureInternal(GraphicsCaptureItem item)
        {
            // 下面参数暂时不能修改
            Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.Create(
                _canvasDevice, // D3D device
                DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format
                // 要在其中存储捕获的框架的缓冲区数量
                1, 
                // 每个缓冲区大小
                item.Size); // Size of the buffers

            framePool.FrameArrived += (s, a) =>
            {
                using (var frame = framePool.TryGetNextFrame())
                {
                    try
                    {
                        // 将获取到的 Direct3D11CaptureFrame 转 win2d 的
                        CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(
                            _canvasDevice,
                            frame.Surface);

                        CanvasComposition.Resize(_surface, canvasBitmap.Size);

                        using (var session = CanvasComposition.CreateDrawingSession(_surface))
                        {
                            session.Clear(Colors.Transparent);
                            session.DrawImage(canvasBitmap);
                        }
                    }
                    catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                    {
                        // 设备丢失
                    }
                }
            };

            var captureSession = framePool.CreateCaptureSession(item);
            captureSession.StartCapture();

            // 作为字段防止内存回收
            _direct3D11CaptureFramePool = framePool;
            _graphicsCaptureSession = captureSession;
        }


        private CanvasDevice _canvasDevice;
        private CompositionDrawingSurface _surface;

        // 下面属性防止内存回收
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Direct3D11CaptureFramePool _direct3D11CaptureFramePool;
        private GraphicsCaptureSession _graphicsCaptureSession;
    }
}