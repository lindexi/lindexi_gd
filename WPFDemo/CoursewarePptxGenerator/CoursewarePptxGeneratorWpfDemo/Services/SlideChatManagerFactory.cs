using System.IO;
using AgentLib;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using CoursewarePptxGeneratorWpfDemo.Rendering;
using CoursewarePptxGeneratorWpfDemo.Threading;
using PptxGenerator;
using PptxGenerator.Evaluation;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Creates configured <see cref="SlideChatManager" /> instances for independent courseware pages.
/// </summary>
public sealed class SlideChatManagerFactory
{
    private const string AgentConfigurationFile = @"C:\lindexi\Work\Key\AgentConfiguration.json";
    private const string DefaultModelName = "deepseek-v4-flash";
    private const string DefaultMcpServiceUrl = "http://127.0.0.1:64773/mcp";

    private readonly WpfDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlideChatManagerFactory" /> class.
    /// </summary>
    public SlideChatManagerFactory()
    {
        _dispatcher = WpfDispatcher.Instance;
    }

    /// <summary>
    /// Creates a configured <see cref="SlideChatManager" /> for one courseware page.
    /// </summary>
    /// <returns>The configured <see cref="SlideChatManager" />.</returns>
    public async Task<SlideChatManager> CreateAsync()
    {
        var copilotChatManager = await CreateCopilotChatManagerAsync(useMainThreadDispatcher: true).ConfigureAwait(false);

        var localPipeline = new SlideMlRenderPipeline(
            new SlideMlLayoutEngine(),
            new CoursewareWpfSlideMlRenderEngine(),
            _dispatcher);
        var renderPipeline = new CoursewareSwitchableSlideMlRenderPipeline(localPipeline);
        _ = await renderPipeline.TryEnableMcpAsync(DefaultMcpServiceUrl).ConfigureAwait(false);

        var slideMlRenderTool = new SlideMlRenderTool(renderPipeline, _dispatcher);

        var evaluatorChatManager = await CreateCopilotChatManagerAsync(useMainThreadDispatcher: false).ConfigureAwait(false);
        var slideEvaluator = new AiSlideEvaluator(evaluatorChatManager);
        var promptEvaluator = new AiPromptEvaluator(evaluatorChatManager);
        var promptOptimizer = new AiPromptOptimizer(evaluatorChatManager);

        return new SlideChatManager(
            copilotChatManager,
            slideMlRenderTool,
            slideEvaluator: slideEvaluator,
            promptEvaluator: promptEvaluator,
            promptOptimizer: promptOptimizer);
    }

    private async Task<CopilotChatManager> CreateCopilotChatManagerAsync(bool useMainThreadDispatcher)
    {
        var copilotChatManager = useMainThreadDispatcher
            ? new CopilotChatManager { MainThreadDispatcher = _dispatcher }
            : new CopilotChatManager();

        var endpointManager = copilotChatManager.AgentApiEndpointManager;
        await endpointManager.LoadConfigurationFromJsonFileAsync(new FileInfo(AgentConfigurationFile)).ConfigureAwait(false);

        var primaryModel = endpointManager.GetModel(DefaultModelName);
        if (primaryModel is null)
        {
            throw new InvalidOperationException("未找到指定的语言模型。");
        }

        endpointManager.PrimaryModel = primaryModel;
        return copilotChatManager;
    }
}
