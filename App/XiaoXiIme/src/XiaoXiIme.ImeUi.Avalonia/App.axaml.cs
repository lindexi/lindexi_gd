using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

public sealed partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = desktop.Args is { Length: > 0 }
                ? new XiaoXiImeIpcOptions(desktop.Args[0])
                : XiaoXiImeIpcOptions.Default;
            var statusFilePath = desktop.Args is { Length: > 1 } ? desktop.Args[1] : null;
            var window = new CandidateWindow();
            var client = new XiaoXiImeIpcClient(options);
            var presenter = new CandidateWindowPresenter(window, client, statusFilePath);

            desktop.MainWindow = window;
            desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => presenter.Dispose();
            presenter.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
