using AgentLib;
using AgentLib.Core;
using PptxGenerator;
using PptxGenerator.Evaluation;
using PptxGenerator.Pipeline;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Creates configured <see cref="SlideChatManager" /> instances for independent courseware pages.
/// </summary>
public sealed class SlideChatManagerFactory : ISlideChatManagerFactory
{
    /// <summary>
    /// Gets the default MCP service URL used by the courseware renderer.
    /// </summary>
    public static string DefaultMcpServiceUrl => "http://127.0.0.1:64773/mcp";

    private readonly WpfDispatcher _dispatcher;
    private readonly ICopilotChatManagerFactory _chatManagerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlideChatManagerFactory" /> class.
    /// </summary>
    public SlideChatManagerFactory()
        : this(new CopilotChatManagerFactory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlideChatManagerFactory" /> class.
    /// </summary>
    /// <param name="chatManagerFactory">The shared language-model chat manager factory.</param>
    public SlideChatManagerFactory(ICopilotChatManagerFactory chatManagerFactory)
    {
        ArgumentNullException.ThrowIfNull(chatManagerFactory);

        _dispatcher = WpfDispatcher.Instance;
        _chatManagerFactory = chatManagerFactory;
    }

    /// <summary>
    /// Creates a configured <see cref="SlideChatManager" /> for one courseware page.
    /// </summary>
    /// <returns>The configured <see cref="SlideChatManager" />.</returns>
    public async Task<SlideChatManager> CreateAsync()
    {
        var copilotChatManager = await _chatManagerFactory.CreateAsync(
            AgentWorkload.SlideGeneration).ConfigureAwait(false);
        var renderPipeline = CreateRenderPipeline();
        _ = Task.Run(async () =>
        {
            _ = await renderPipeline.TryEnableMcpAsync(DefaultMcpServiceUrl).ConfigureAwait(false);
        });

        var slideMlRenderTool = CreateRenderTool(renderPipeline);

        var evaluatorChatManager = await _chatManagerFactory.CreateAsync(
            AgentWorkload.Evaluation).ConfigureAwait(false);
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

    /// <inheritdoc />
    public SlideChatManager CreateFallback()
    {
        var copilotChatManager = new CopilotChatManager { MainThreadDispatcher = _dispatcher };
        return new SlideChatManager(copilotChatManager, CreateRenderTool(CreateRenderPipeline()));
    }

    private SwitchableSlideMlRenderPipeline CreateRenderPipeline()
    {
        var localPipeline = new SlideMlRenderPipeline(
            new SlideMlLayoutEngine(),
            new WpfSlideMlRenderEngine(),
            _dispatcher);
        return new SwitchableSlideMlRenderPipeline(localPipeline);
    }

    private SlideMlRenderTool CreateRenderTool(ISlideMlRenderPipeline renderPipeline)
    {
        return new SlideMlRenderTool(renderPipeline, _dispatcher);
    }

}
