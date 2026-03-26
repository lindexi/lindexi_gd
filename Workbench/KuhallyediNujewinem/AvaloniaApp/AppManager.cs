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
using Avalonia.Controls.Embedding.Offscreen;
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

            // https://github.com/AvaloniaUI/Avalonia/issues/2174#issuecomment-3030306384
            var offscreenTopLevelImpl = new OffscreenTopLevelImpl()
            {
                ClientSize = new Size(1000, 600)
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
        });

        return imageFilePath;
    }
}

class OffscreenTopLevelImpl : OffscreenTopLevelImplBase, ITopLevelImpl
{
    public override IEnumerable<object> Surfaces { get; } = [];
    public override IMouseDevice MouseDevice { get; } = new MouseDevice();
}