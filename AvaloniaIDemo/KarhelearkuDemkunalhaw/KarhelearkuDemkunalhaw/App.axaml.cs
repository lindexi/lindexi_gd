using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using KarhelearkuDemkunalhaw.Views;
using Path = System.IO.Path;

namespace KarhelearkuDemkunalhaw;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
            };

            var embeddableControlRoot = new EmbeddableControlRoot();
            embeddableControlRoot.Width = 1000;
            embeddableControlRoot.Height = 600;
            var mainView = new MainView
            {
            };
            embeddableControlRoot.Content = mainView;
            mainView.Loaded += (sender, args) =>
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await Task.Delay(1000); // 等待1秒钟，确保控件已完全加载
                    var renderTargetBitmap =
                        new RenderTargetBitmap(new PixelSize((int)mainView.Bounds.Width, (int)mainView.Bounds.Height));
                    renderTargetBitmap.Render(mainView);

                    var file = Path.Join(AppContext.BaseDirectory, "1.png");
                    renderTargetBitmap.Save(file);
                }, DispatcherPriority.Render);
            };
            embeddableControlRoot.Prepare(); // 调用此方法会触发 Loaded 事件
            embeddableControlRoot.StartRendering();

        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
