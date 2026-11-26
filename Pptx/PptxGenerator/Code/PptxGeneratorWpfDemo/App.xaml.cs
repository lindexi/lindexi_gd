using AgentLib;
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

        SlideChatManager slideChatManager;
        try
        {
            slideChatManager = await CreateSlideChatManagerAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "无法启动幻灯片演示", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var viewModel = new MainWindowViewModel(slideChatManager);
        var mainWindow = new MainWindow
        {
            DataContext = viewModel,
        };
        mainWindow.Show();

        await viewModel.TryConnectMcpRenderAsync();
    }

    /// <summary>
    /// 从 Agent 配置文件创建使用 Responses API 的幻灯片生成器。
    /// </summary>
    private static async Task<SlideChatManager> CreateSlideChatManagerAsync()
    {
        var agentConfigurationFile = Environment.GetEnvironmentVariable("PPTX_GENERATOR_AGENT_CONFIG");
        if (string.IsNullOrWhiteSpace(agentConfigurationFile))
            agentConfigurationFile = Path.Combine(AppContext.BaseDirectory, "AgentConfiguration.json");

        var configuration = await AgentApiManagerConfiguration.FromJsonFileAsync(new FileInfo(agentConfigurationFile));

        var dispatcher = WpfDispatcher.Instance;
        var copilotChatManager = new CopilotChatManager
        {
            MainThreadDispatcher = dispatcher,
        };
        var agentApiEndpointManager = copilotChatManager.AgentApiEndpointManager;
        agentApiEndpointManager.LoadConfiguration(configuration);
        if (agentApiEndpointManager.PrimaryModel is not IResponsesClientProvider)
            throw new InvalidOperationException("请在 AgentConfiguration.json 中将 PrimaryModel 配置为支持 Responses API 的模型。");

        var defaultPipeline = new SlideMlRenderPipeline(new SlideMlLayoutEngine(), new WpfSlideMlRenderEngine(), dispatcher);
        var renderPipeline = new SwitchableSlideMlRenderPipeline(defaultPipeline);
        var slideMlRenderTool = new SlideMlRenderTool(renderPipeline, dispatcher);

        var evaluatorChatManager = new CopilotChatManager();
        evaluatorChatManager.AgentApiEndpointManager.LoadConfiguration(configuration);

        var slideEvaluator = new AiSlideEvaluator(evaluatorChatManager, useResponsesApi: true);
        var promptEvaluator = new AiPromptEvaluator(evaluatorChatManager, useResponsesApi: true);
        var promptOptimizer = new AiPromptOptimizer(evaluatorChatManager, useResponsesApi: true);

        return new SlideChatManager(copilotChatManager, slideMlRenderTool, new PipelineConfiguration
        {
            UseResponsesApi = true,
            SlideEvaluator = slideEvaluator,
            PromptEvaluator = promptEvaluator,
            PromptOptimizer = promptOptimizer,
        });
    }
}
