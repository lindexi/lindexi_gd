using System.IO;
using System.Windows;

using LeefayjehekijawlalWhichayfawcelhega.Models;
using LeefayjehekijawlalWhichayfawcelhega.Services;
using LeefayjehekijawlalWhichayfawcelhega.ViewModels;

namespace LeefayjehekijawlalWhichayfawcelhega;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DoubaoOptions doubaoOptions = new()
        {
            Endpoint = "https://ark.cn-beijing.volces.com/api/v3",
            PromptModelId = "ep-20260306101224-c8mtg",
            ImageModelId = "ep-20260120102721-c4pxb",
            ApiKeyEnvironmentVariableName = "DOUBAO_API_KEY",
        };

        MainViewModel mainViewModel = new(
            new DoubaoPromptAgentService(doubaoOptions),
            new DoubaoImageGenerationService(doubaoOptions),
            new SlideOutlineParser(),
            new ImageExportService())
        {
            ExportRootDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "AiCoursewareExports"),
        };

        MainWindow mainWindow = new()
        {
            DataContext = mainViewModel,
        };

        mainWindow.Show();
    }
}
