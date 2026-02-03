using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using RallreakechuFeakenalldea.Views;

using SimpleWrite.Views;

using System;
using System.IO;
using System.Threading.Tasks;

using Path = System.IO.Path;

namespace RallreakechuFeakenalldea;

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

            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1)
            {
                // 第 0 个参数是可执行文件路径
                var filePath = commandLineArgs[1];
                if (File.Exists(filePath))
                {
                    if (desktop.MainWindow.Content is SimpleWriteMainView mainViewControl)
                    {
                        _ = mainViewControl.OpenFileAsync(new FileInfo(filePath));
                    }
                }
            }
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
