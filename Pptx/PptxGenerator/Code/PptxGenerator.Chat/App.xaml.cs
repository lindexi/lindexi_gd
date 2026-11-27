using System.Globalization;
using System.IO;
using System.Windows;
using PptxGenerator.Chat.Services;
using PptxGenerator.Chat.ViewModels;

namespace PptxGenerator.Chat;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ChatDiagnostics.StartFileLog(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PptxGenerator.Chat", "Logs"));
        var models = await ChatModels.LoadAsync();
        var viewModel = new ChatViewModel(models, Dispatcher);
        MainWindow = new MainWindow { DataContext = viewModel };
        MainWindow.Show();
        await viewModel.CheckStartupMcpAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ChatDiagnostics.StopFileLog();
        base.OnExit(e);
    }
}

internal static class UiText
{
    internal static string Get(string key)
    {
        var app = Application.Current;
        return app.Dispatcher.CheckAccess()
            ? (string)app.FindResource(key)
            : app.Dispatcher.Invoke(() => (string)app.FindResource(key));
    }
    internal static string Format(string key, string value) => string.Format(CultureInfo.CurrentCulture, Get(key), value);
}
