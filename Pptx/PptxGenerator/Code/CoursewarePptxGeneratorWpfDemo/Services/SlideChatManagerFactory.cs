using AgentLib;
using AgentLib.Core;
using PptxGenerator;
using PptxGenerator.Evaluation;
using PptxGenerator.Models;
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
    /// <param name="options">The page runtime options. When omitted, the default document context is used.</param>
    /// <param name="cancellationToken">The token used to cancel initialization.</param>
    /// <returns>The configured <see cref="SlideChatManager" />.</returns>
    public async Task<SlideChatManager> CreateAsync(
        SlideChatManagerFactoryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var documentContext = options?.DocumentContext ?? new SlideDocumentContext();
        var copilotChatManager = await _chatManagerFactory.CreateAsync(
            AgentWorkload.SlideGeneration,
            cancellationToken).ConfigureAwait(false);
        var renderPipeline = CreateRenderPipeline(documentContext);
        if (options?.TryEnableDefaultMcp != false)
        {
            _ = TryEnableDefaultMcpAsync(renderPipeline, cancellationToken);
        }

        var slideMlRenderTool = CreateRenderTool(renderPipeline);

        var evaluatorChatManager = await _chatManagerFactory.CreateAsync(
            AgentWorkload.Evaluation,
            cancellationToken).ConfigureAwait(false);
        var slideEvaluator = new AiSlideEvaluator(evaluatorChatManager);
        var promptEvaluator = new AiPromptEvaluator(evaluatorChatManager);
        var promptOptimizer = new AiPromptOptimizer(evaluatorChatManager);

        return new SlideChatManager(
            copilotChatManager,
            slideMlRenderTool,
            slideDocumentContext: documentContext,
            slideEvaluator: slideEvaluator,
            promptEvaluator: promptEvaluator,
            promptOptimizer: promptOptimizer);
    }

    /// <inheritdoc />
    public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
    {
        var documentContext = options?.DocumentContext ?? new SlideDocumentContext();
        var copilotChatManager = new CopilotChatManager { MainThreadDispatcher = _dispatcher };
        return new SlideChatManager(
            copilotChatManager,
            CreateRenderTool(CreateRenderPipeline(documentContext)),
            slideDocumentContext: documentContext);
    }

    private SwitchableSlideMlRenderPipeline CreateRenderPipeline(SlideDocumentContext documentContext)
    {
        ArgumentNullException.ThrowIfNull(documentContext);
        var localPipeline = new SlideMlRenderPipeline(
            new SlideMlLayoutEngine(),
            new WpfSlideMlRenderEngine(),
            _dispatcher,
            new SlideMlPipelineContext(documentContext));
        return new SwitchableSlideMlRenderPipeline(localPipeline);
    }

    private SlideMlRenderTool CreateRenderTool(ISlideMlRenderPipeline renderPipeline)
    {
        return new SlideMlRenderTool(renderPipeline, _dispatcher);
    }

    private static async Task TryEnableDefaultMcpAsync(
        SwitchableSlideMlRenderPipeline renderPipeline,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await renderPipeline.TryEnableMcpAsync(
                DefaultMcpServiceUrl,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            // MCP is an optional rendering enhancement. Initialization failures keep local rendering active.
        }
    }

}
