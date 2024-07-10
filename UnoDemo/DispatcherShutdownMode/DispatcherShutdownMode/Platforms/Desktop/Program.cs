using Uno.UI.Runtime.Skia;

namespace DispatcherShutdownMode;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();

        Console.WriteLine($"应用退出，预期此时消息循环还没退出");

        var app = (App)Application.Current;
        app.DispatcherQueue.TryEnqueue(() =>
        {
            Console.WriteLine($"进入消息循环");

            var window = new Window();
            var frame = new Frame();
            window.Content = frame;
            frame.Navigate(typeof(MainPage));
        });

        // 防止进程退出
        Console.Read();
    }
}
