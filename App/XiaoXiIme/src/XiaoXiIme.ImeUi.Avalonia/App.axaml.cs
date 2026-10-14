using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

public sealed partial class App : Application
{
    internal static ProductUiEntry? ProductEntry { get; set; }

    /// <summary>Opens a product settings session without starting a Host; dispose it when the embedding UI exits.</summary>
    public IDisposable OpenProductSettings(XiaoXiImeIpcOptions? options = null)
    {
        var product = new ProductUiController(options ?? XiaoXiImeIpcOptions.Default);
        product.OpenSettings();
        return product;
    }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { Args: null or { Length: 0 } } standalone)
        {
            standalone.MainWindow = new DictionaryDebugWindow();
            standalone.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnMainWindowClose;
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ProductValidationBounds.Start(desktop);
            var args = desktop.Args ?? [];
            var openSettings = args.Contains("--settings", StringComparer.Ordinal);
            var positional = args.Where(arg => arg is not "--settings" and not "--exit").ToArray();
            var options = positional.Length > 0 ? new XiaoXiImeIpcOptions(positional[0]) : XiaoXiImeIpcOptions.Default;
            var statusFilePath = positional.Length > 1 ? positional[1] : null;
            var product = new ProductUiController(options);
            desktop.Exit += (_, _) => product.Dispose();
            ProductEntry?.Attach(async settings =>
            {
                if (settings) product.OpenSettings();
                else await product.ChangeVisibilityAsync(true);
            }, () => desktop.Shutdown());
            if (openSettings) product.OpenSettings();
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
