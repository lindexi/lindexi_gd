using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Avalonia;

using Microsoft.Extensions.AI;

using System;
using System.IO;
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

        var chatClient = await CreateChatClientFromAgentConfigAsync().ConfigureAwait(false);
        if (chatClient is null)
        {
            return 1;
        }

        var slideRenderer = new SlideRenderer();
        var slideGenerationService = new SlideGenerationService(chatClient, slideRenderer);
        var cliRunner = new SlideCliRunner(slideGenerationService);
        return await cliRunner.RunAsync(prompt).ConfigureAwait(false);
    }

    /// <summary>
    /// 从 Agent 配置文件加载模型并创建 <see cref="IChatClient"/>。
    /// 供 CLI 和 GUI 路径共用。
    /// </summary>
    /// <returns>创建成功的 <see cref="IChatClient"/>；如果模型未找到则返回 null。</returns>
    public static async Task<IChatClient?> CreateChatClientFromAgentConfigAsync()
    {
        var agentConfigurationFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";

        AgentApiManagerConfiguration agentApiManagerConfiguration = await AgentApiManagerConfiguration.FromJsonFileAsync(new FileInfo(agentConfigurationFile)).ConfigureAwait(false);

        var agentApiEndpointManager = new AgentApiEndpointManager();
        agentApiEndpointManager.LoadConfiguration(agentApiManagerConfiguration);

        ILanguageModel? languageModel = agentApiEndpointManager.GetModel("qwen3.7-plus");
        if (languageModel is null)
        {
            Console.Error.WriteLine("未找到指定的语言模型。");
            return null;
        }

        return await languageModel.GetChatClientAsync().ConfigureAwait(false);
    }
}