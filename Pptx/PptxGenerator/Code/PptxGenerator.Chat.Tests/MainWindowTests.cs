using System.Windows.Threading;
using PptxGenerator.Chat.Services;
using PptxGenerator.Chat.ViewModels;

namespace PptxGenerator.Chat.Tests;

[TestClass]
public sealed class MainWindowTests
{
    [STATestMethod]
    public void StartupWithoutModelsShowsConfigurationNotice()
    {
        var app = new App();
        app.InitializeComponent();
        var model = new ChatViewModel(new ChatModels(Array.Empty<ChatModel>(), null, "NoModels"), Dispatcher.CurrentDispatcher);
        var window = new MainWindow { DataContext = model };
        try
        {
            window.Show();
            window.UpdateLayout();

            Assert.AreEqual((true, false, app.FindResource("NoModels") as string),
                (window.IsVisible, model.SendCommand.CanExecute(null), model.ModelNotice));
        }
        finally
        {
            window.Close();
        }
    }
}
