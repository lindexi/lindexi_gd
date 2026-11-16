using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

public sealed partial class App : Application
{
    internal static ProductUiEntry? ProductEntry { get; set; }
    internal static Exception? StartupError { get; set; }

    private static readonly Dictionary<string, DictionaryDebugWindow> ExperienceWindows =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Overrides the experience settings file for an embedded application instance.</summary>
    public string? ExperienceSettingsPath { get; init; }

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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime standalone &&
            (standalone.Args is null or { Length: 0 } || standalone.Args[0] is "--dictionary-debug" or "--experience"))
        {
            var debugArgs = standalone.Args ?? [];
            string? settingsPath = ExperienceSettingsPath;
            string? packagePath = null;
            for (var i = 1; i < debugArgs.Length; i++)
            {
                if (debugArgs[i] == "--settings")
                {
                    continue;
                }

                if (debugArgs[i] == "--debug-settings-path" && i + 1 < debugArgs.Length && Path.IsPathFullyQualified
                        (debugArgs[i + 1]))
                {
                    settingsPath = debugArgs[++i];
                    continue;
                }

                if (debugArgs[0] == "--dictionary-debug" && debugArgs[i] == "--debug-package-path" &&
                    i + 1 < debugArgs.Length && Path.IsPathFullyQualified(debugArgs[i + 1]))
                {
                    packagePath = debugArgs[++i];
                    continue;
                }

                throw new ArgumentException(ProductResources.Text("InvalidSettings"));
            }

            ProductValidationBounds.Start(standalone);
            var profile = DictionaryDebugSettings.ResolvePath(settingsPath);
            try
            {
                if (StartupError is not null)
                {
                    ShowStartupError(standalone, StartupError, profile);
                }
                else if (ExperienceWindows.TryGetValue(profile, out var existing))
                {
                    standalone.MainWindow = existing;
                    existing.OpenFromEntry(debugArgs.Contains("--settings"));
                }
                else
                {
                    var window = new DictionaryDebugWindow(profile, debugArgs.Contains("--settings"), packagePath);
                    ExperienceWindows.Add(profile, window);
                    window.Closed += (_, _) => ExperienceWindows.Remove(profile);
                    standalone.MainWindow = window;
                    ProductEntry?.Attach(window.OpenFromEntry, () => window.Close());
                }
            }
            catch (IOException exception)
            {
                ShowStartupError(standalone, exception, profile);
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowStartupError(standalone, exception, profile);
            }

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
            ProductEntry?.Attach
            (
                async settings =>
                {
                    if (settings)
                    {
                        product.OpenSettings();
                    }
                    else
                    {
                        await product.ChangeVisibilityAsync(true);
                    }
                }, () => desktop.Shutdown()
            );
            if (openSettings)
            {
                product.OpenSettings();
            }

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

    private static void ShowStartupError
        (IClassicDesktopStyleApplicationLifetime lifetime, Exception exception, string profile)
    {
        System.Diagnostics.Trace.WriteLine
            ($"Experience startup: profile={profile}; HRESULT=0x{exception.HResult:X8}; {exception}");
        var conflict = exception is IOException && (exception.HResult & 0xffff) is 32 or 33;
        var close = new global::Avalonia.Controls.Button { Content = ProductResources.Text("OK") };
        var panel = new global::Avalonia.Controls.StackPanel { Margin = new Thickness(24), Spacing = 16 };
        panel.Children.Add
        (
            new global::Avalonia.Controls.TextBlock
            {
                Text = ProductResources.Text
                           (conflict ? "ExperienceWriterBusy" : "ExperienceStartupUnavailable") + "\n" +
                       string.Format
                       (
                           ProductResources.Text("ExperienceStartupDetails"), profile, exception.HResult,
                           exception.Message
                       ),
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            }
        );
        panel.Children.Add(close);
        var window = new global::Avalonia.Controls.Window
        {
            Name = "ExperienceStartupError", Title = ProductResources.Text("Title"),
            Width = 580, Height = 240, Content = panel
        };
        close.Click += (_, _) => window.Close();
        lifetime.MainWindow = window;
        ProductEntry?.Attach
        (
            _ =>
            {
                window.Show();
                window.Activate();
            }, () => window.Close()
        );
    }
}