using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using PptxGenerator;

using System;
using System.IO;
using System.Windows;
using PptxGenerator.Evaluation;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;

namespace PptxGeneratorWpfDemo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var slideChatManager = await CreateSlideChatManagerAsync();
        if (slideChatManager is null)
        {
            Shutdown(1);
            return;
        }

        var viewModel = new MainWindowViewModel(slideChatManager);
        var mainWindow = new MainWindow
        {
            DataContext = viewModel,
        };
        mainWindow.Show();
    }

    /// <summary>
    /// 从 Agent 配置文件创建 <see cref="SlideChatManager"/>，供 GUI 和 CLI 路径共用。
    /// </summary>
    /// <returns>创建成功的 <see cref="SlideChatManager"/>；如果失败则返回 null。</returns>
    private static async Task<SlideChatManager?> CreateSlideChatManagerAsync()
    {
        var agentConfigurationFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";

        var dispatcher = WpfDispatcher.Instance;
        var copilotChatManager = new CopilotChatManager { MainThreadDispatcher = dispatcher };
        var agentApiEndpointManager = copilotChatManager.AgentApiEndpointManager;
        await agentApiEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(agentConfigurationFile));

        ILanguageModel? languageModel = agentApiEndpointManager.GetModel("deepseek-v4-flash");
        if (languageModel is null)
        {
            Console.Error.WriteLine("未找到指定的语言模型。");
            return null;
        }

        agentApiEndpointManager.PrimaryModel = languageModel;

        var SlideMlRenderPipeline = new SlideMlRenderPipeline(new SlideMlLayoutEngine(), new WpfSlideMlRenderEngine(), dispatcher);
        var SlideMlRenderTool = new SlideMlRenderTool(SlideMlRenderPipeline, dispatcher);

        var evaluatorChatManager = new CopilotChatManager();
        var evaluatorEndpointManager = evaluatorChatManager.AgentApiEndpointManager;
        await evaluatorEndpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(agentConfigurationFile));

        ILanguageModel? evaluatorModel = evaluatorEndpointManager.GetModel("deepseek-v4-flash")
            ?? languageModel;
        evaluatorEndpointManager.PrimaryModel = evaluatorModel;

        var slideEvaluator = new AiSlideEvaluator(evaluatorChatManager);
        var promptEvaluator = new AiPromptEvaluator(evaluatorChatManager);
        var promptOptimizer = new AiPromptOptimizer(evaluatorChatManager);

        return new SlideChatManager(copilotChatManager, SlideMlRenderTool, slideEvaluator: slideEvaluator, promptEvaluator: promptEvaluator, promptOptimizer: promptOptimizer);
    }
}
