using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PptxGenerator.Evaluation;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;

using Avalonia;

using System;
using System.Collections.Generic;
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

        string? modelName = null;
        var promptParts = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--model" && i + 1 < args.Length)
            {
                modelName = args[i + 1];
                i++; // skip value
            }
            else if (!string.IsNullOrWhiteSpace(args[i]))
            {
                promptParts.Add(args[i]);
            }
        }

        var prompt = string.Join(' ', promptParts);

        var slideChatManager = await CreateSlideChatManagerAsync(modelName).ConfigureAwait(false);
        if (slideChatManager is null)
        {
            return 1;
        }

        var cliRunner = new SlideCliRunner(slideChatManager);
        return await cliRunner.RunAsync(prompt).ConfigureAwait(false);
    }

    /// <summary>
    /// 从 Agent 配置文件创建 <see cref="SlideChatManager"/>，供 GUI 和 CLI 路径共用。
    /// 整个应用共用同一个 <see cref="CopilotChatManager"/>，评估者也复用此实例。
    /// </summary>
    /// <param name="modelName">可选的主模型名称。为 <see langword="null"/> 时使用默认 "deepseek-v4-flash"。</param>
    /// <returns>创建成功的 <see cref="SlideChatManager"/>；如果失败则返回 <see langword="null"/>。</returns>
    public static async Task<SlideChatManager?> CreateSlideChatManagerAsync(string? modelName = null)
    {
        var agentConfigurationFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";

        var dispatcher = AvaloniaDispatcher.Instance;
        var copilotChatManager = new CopilotChatManager { MainThreadDispatcher = dispatcher };
        var agentApiEndpointManager = copilotChatManager.AgentApiEndpointManager;
        await agentApiEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(agentConfigurationFile)).ConfigureAwait(false);

        ILanguageModel? languageModel = agentApiEndpointManager.GetModel(modelName ?? "deepseek-v4-flash");
        if (languageModel is null)
        {
            Console.Error.WriteLine("未找到指定的语言模型。");
            return null;
        }

        agentApiEndpointManager.PrimaryModel = languageModel;

        var defaultPipeline = new SlideMlRenderPipeline(new SlideMlLayoutEngine(), new AvaloniaSlideRenderEngine(), dispatcher);
        var renderPipeline = new SwitchableSlideMlRenderPipeline(defaultPipeline);
        var slideMlRenderTool = new SlideMlRenderTool(renderPipeline, dispatcher);

        // 评估者和优化者复用同一个 CopilotChatManager，共用主模型
        var slideEvaluator = new AiSlideEvaluator(copilotChatManager);
        var promptEvaluator = new AiPromptEvaluator(copilotChatManager);
        var promptOptimizer = new AiPromptOptimizer(copilotChatManager);

        return new SlideChatManager(copilotChatManager, slideMlRenderTool,
            slideEvaluator: slideEvaluator,
            promptEvaluator: promptEvaluator,
            promptOptimizer: promptOptimizer);
    }
}
