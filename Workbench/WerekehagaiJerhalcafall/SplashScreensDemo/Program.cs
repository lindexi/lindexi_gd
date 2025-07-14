using System.IO;
using SplashScreensLib.SplashScreens;

namespace SplashScreensDemo;

static class Program
{
    [System.STAThread]
    public static void Main(string[] args)
    {
        var splashScreenFile = new FileInfo("SplashScreen.png");
        SplashScreensLib.SplashScreens.SplashScreen splashScreen = new SplashScreensLib.SplashScreens.SplashScreen(splashScreenFile);
        _ = splashScreen.ShowAsync();

        var app = new App();
        app.InitializeComponent();
        app.Startup += async (sender, eventArgs) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            splashScreen.Close();
        };
        app.Run();
    }
}