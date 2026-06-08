using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Avalonia;

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

        var slideChatManager = await CreateSlideChatManagerAsync().ConfigureAwait(false);
        if (slideChatManager is null)
        {
            return 1;
        }

        var cliRunner = new SlideCliRunner(slideChatManager);
        return await cliRunner.RunAsync(prompt).ConfigureAwait(false);
    }

    /// <summary>
    /// 从 Agent 配置文件创建 <see cref="SlideChatManager"/>，供 GUI 和 CLI 路径共用。
    /// </summary>
    /// <returns>创建成功的 <see cref="SlideChatManager"/>；如果失败则返回 null。</returns>
    public static async Task<SlideChatManager?> CreateSlideChatManagerAsync()
    {
        var agentConfigurationFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";

        var copilotChatManager = new CopilotChatManager();
        var agentApiEndpointManager = copilotChatManager.AgentApiEndpointManager;
        await agentApiEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(agentConfigurationFile)).ConfigureAwait(false);

        ILanguageModel? languageModel = agentApiEndpointManager.GetModel("qwen3.7-plus");
        if (languageModel is null)
        {
            Console.Error.WriteLine("未找到指定的语言模型。");
            return null;
        }

        agentApiEndpointManager.PrimaryModel = languageModel;

        var slideRenderer = new SlideRenderer();
        var slideRenderTool = new SlideRenderTool(slideRenderer);

        // 创建评估者（使用独立 CopilotChatManager，可配置不同模型）
        var evaluatorChatManager = new CopilotChatManager();
        var evaluatorEndpointManager = evaluatorChatManager.AgentApiEndpointManager;
        await evaluatorEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(agentConfigurationFile)).ConfigureAwait(false);

        // 评估者可选用更便宜的模型
        ILanguageModel? evaluatorModel = evaluatorEndpointManager.GetModel("qwen3.7-plus")
            ?? languageModel;
        evaluatorEndpointManager.PrimaryModel = evaluatorModel;

        var slideEvaluator = new AiSlideEvaluator(evaluatorChatManager);
        var promptEvaluator = new AiPromptEvaluator(evaluatorChatManager);

        return new SlideChatManager(copilotChatManager, slideRenderTool, slideEvaluator, promptEvaluator);
    }
}