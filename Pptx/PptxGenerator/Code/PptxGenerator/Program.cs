using Avalonia;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace PptxGenerator;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int/*为什么不是 Task 的返回类型？因为 STAThread 的原因，切 Task 会导致 STA 失效 https://github.com/dotnet/runtime/issues/73099 */ Main(string[] args)
    {
        if (args.Length > 0)
        {
            return RunCliAsync(args).Result;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static async Task<int> RunCliAsync(string[] args)
    {
        BuildAvaloniaApp().SetupWithoutStarting();
        var prompt = string.Join(' ', args.Where(t => !string.IsNullOrWhiteSpace(t)));

        var chatClientCreator = new ChatClientCreator();
        var slideRenderer = new SlideRenderer();
        var slideGenerationService = new SlideGenerationService(chatClientCreator, slideRenderer);
        var cliRunner = new SlideCliRunner(slideGenerationService);
        return await cliRunner.RunAsync(prompt).ConfigureAwait(false);
    }
}