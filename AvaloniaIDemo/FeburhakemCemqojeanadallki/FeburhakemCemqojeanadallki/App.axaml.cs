using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using FeburhakemCemqojeanadallki.Views;
using Path = System.IO.Path;

namespace FeburhakemCemqojeanadallki;

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
                mainView.Background = Brushes.White;
                var renderTargetBitmap =
                    new RenderTargetBitmap(new PixelSize((int) mainView.Bounds.Width, (int) mainView.Bounds.Height));
                renderTargetBitmap.Render(mainView);

                var file = Path.Join(AppContext.BaseDirectory, "1.png");
                renderTargetBitmap.Save(file);
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
