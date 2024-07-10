using Microsoft.UI;
using Uno.UI.Runtime.Skia;

namespace DispatcherShutdownMode;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                Console.ReadLine();

                var app = (App) Application.Current;
                app.DispatcherQueue.TryEnqueue(() =>
                {
                    Console.WriteLine($"进入消息循环 线程 {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name}");

                    var window = new Window();
                    var frame = new Frame();
                    window.Content = frame;
                    frame.Navigate(typeof(MainPage));
                    window.Activate();
                });
            }
        });
        thread.Start();

        App.InitializeLogging();

        Thread.CurrentThread.Name = "Main";
        Console.WriteLine($"线程 {Thread.CurrentThread.ManagedThreadId}");

        var host = SkiaHostBuilder.Create()
            .App(() =>
            {
                var app = new App();

                Console.WriteLine($"App Thread={Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name}");

                //app.Launched += (sender, eventArgs) =>
                //{
                //    Task.Run(() =>
                //    {
                //        Console.ReadLine();
                //        Console.WriteLine("尝试启动窗口");

                //        app.DispatcherQueue.TryEnqueue(() =>
                //        {
                //            var window = new Window();
                //            window.Content = new Grid()
                //            {
                //                Background = new SolidColorBrush(Colors.Blue),
                //            };
                //            window.Activate();
                //        });
                //    });
                //};

                return app;
            })
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();

        Console.WriteLine($"应用退出，预期此时消息循环还没退出");
    }
}
