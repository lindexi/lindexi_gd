using Avalonia.Controls.Embedding;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
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
                var embeddableControlRoot = new EmbeddableControlRoot();
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