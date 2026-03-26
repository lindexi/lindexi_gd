using Avalonia;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Shapes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Rendering.Composition;
using Path = System.IO.Path;

namespace AvaloniaApp;

public class AppManager
{
    public async Task<string> TakeAsync()
    {
        await App.WaitAppLaunched();

        var imageFilePath = Path.Join(Path.GetTempPath(), $"{Path.GetRandomFileName()}.png");

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var taskCompletionSource = new TaskCompletionSource();

            if (OperatingSystem.IsWindows())
            {
                // https://github.com/AvaloniaUI/Avalonia/issues/2174#issuecomment-3030306384
                var offscreenTopLevelImpl = new OffscreenTopLevelImpl()
                {
                    ClientSize = new Size(1000,600)
                };
                var embeddableControlRoot = new EmbeddableControlRoot(offscreenTopLevelImpl);
                embeddableControlRoot.Width = 1000;
                embeddableControlRoot.Height = 600;
                var mainView = new MainView();
                mainView.Loaded += (sender, args) =>
                {
                    using var renderTargetBitmap = new RenderTargetBitmap(new PixelSize(1000, 600));
                    renderTargetBitmap.Render(mainView);
                    renderTargetBitmap.Save(imageFilePath);
                    taskCompletionSource.SetResult();
                };
                embeddableControlRoot.Content = mainView;

                // 准备离屏渲染工作
                embeddableControlRoot.Prepare(); // 调用此方法会触发 Loaded 事件
                embeddableControlRoot.StartRendering();

                await taskCompletionSource.Task;

                embeddableControlRoot.StopRendering();
                embeddableControlRoot.Dispose();
            }
            else
            {
                // 不能用 EmbeddableControlRoot 的方式，因为在非 Windows 上会抛出异常

                var mainView = new MainView()
                {
                    Width = 1000,
                    Height = 600,
                };
                mainView.Loaded += (sender, args) =>
                {
                    Console.WriteLine("MainView.Loaded");
                };

                using var renderTargetBitmap = new RenderTargetBitmap(new PixelSize(1000, 600));
                renderTargetBitmap.Render(mainView);
                renderTargetBitmap.Save(imageFilePath);
                taskCompletionSource.SetResult();
            }
        });

        return imageFilePath;
    }
}

class OffscreenTopLevelImpl : ITopLevelImpl
{
    public void Dispose()
    {
        
    }

    public object? TryGetFeature(Type featureType)
    {
        return null;
    }

    public void SetInputRoot(IInputRoot inputRoot)
    {
    }

    public Point PointToClient(PixelPoint point)
    {
        return point.ToPoint(1);
    }

    public PixelPoint PointToScreen(Point point)
    {
        return new PixelPoint((int)point.X, (int)point.Y);
    }

    public void SetCursor(ICursorImpl? cursor)
    {
    }

    public IPopupImpl? CreatePopup()
    {
        return null;
    }

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
    }

    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
    }

    public double DesktopScaling { get; set; } = 1;
    public IPlatformHandle? Handle { get; set; }
    public Size ClientSize { get; set; }
    public double RenderScaling { get; set; } = 1;
    public IEnumerable<object> Surfaces { get; set; } = [];
    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
    public Compositor Compositor { get; set; } = new Compositor(null, true);
    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }
    public WindowTransparencyLevel TransparencyLevel { get; set; }
    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; set; }
}