using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;

namespace PptxGenerator.Streaming;

/// <summary>
/// 流式渲染编排管道，将片段提取器、合并器和渲染服务串联起来。
/// 从 LLM 流式输出接收增量文本，提取完整 XML 片段，合并到 DOM 树，在流结束时渲染。
/// </summary>
public sealed class SlideStreamingPipeline
{
    private readonly SlideMlStreamingMerger _merger;
    private readonly SlideStreamRenderService _renderService;
    private readonly ISlideMlPromptProvider _promptProvider;
    private readonly IMainThreadDispatcher _dispatcher;
    private SlideMlFragmentExtractor _extractor = new();

    /// <summary>
    /// 初始化 <see cref="SlideStreamingPipeline"/> 的新实例。
    /// </summary>
    /// <param name="promptProvider">提示词提供者。</param>
    /// <param name="renderPipeline">SlideML 渲染管道。</param>
    /// <param name="dispatcher">主线程调度器。</param>
    public SlideStreamingPipeline(
        ISlideMlPromptProvider promptProvider,
        ISlideMlRenderPipeline renderPipeline,
        IMainThreadDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(promptProvider);
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _promptProvider = promptProvider;
        _dispatcher = dispatcher;
        _merger = new SlideMlStreamingMerger();
        _renderService = new SlideStreamRenderService(renderPipeline, dispatcher);
        _renderService.Rendered += result => Rendered?.Invoke(result);
    }

    /// <summary>
    /// 片段接收事件，每次从 LLM 流中提取到新片段时触发。
    /// </summary>
    public event Action<string>? FragmentReceived;

    /// <summary>
    /// 渲染完成事件。
    /// </summary>
    public event Action<SlideStreamRenderResult>? Rendered;

    /// <summary>
    /// 流式输出完成事件。
    /// </summary>
    public event Action<string>? StreamCompleted;

    /// <summary>
    /// 流式中断事件。
    /// </summary>
    public event Action<string>? StreamInterrupted;

    /// <summary>
    /// 当前合并的 XML。
    /// </summary>
    public string CurrentMergedXml => _merger.GetMergedXml();

    /// <summary>
    /// 处理 LLM 流式增量文本。
    /// </summary>
    /// <param name="text">增量文本。</param>
    /// <param name="context">渲染上下文。</param>
    public void ProcessIncrementalText(string text, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(context);

        _extractor.Append(text);
        var fragments = _extractor.TryExtractFragments();

        foreach (var fragment in fragments)
        {
            _merger.AcceptFragment(fragment, context);
            FragmentReceived?.Invoke(fragment);
        }
    }

    /// <summary>
    /// 处理流结束，渲染合并后的 XML。
    /// </summary>
    /// <param name="context">渲染上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ProcessStreamEndAsync(SlideMlPipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 处理缓冲区中剩余的内容（容错）
        var remaining = _extractor.GetRemaining();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            context.AddWarning($"[Warning] 流结束时缓冲区有未完成的内容: {remaining}");
        }

        var mergedXml = _merger.GetMergedXml();
        if (!string.IsNullOrWhiteSpace(mergedXml))
        {
            await _renderService.RenderAsync(mergedXml, cancellationToken).ConfigureAwait(false);
        }

        StreamCompleted?.Invoke(mergedXml);
    }

    /// <summary>
    /// 重置状态，清空片段提取器缓冲区和合并器 DOM 树。
    /// </summary>
    public void Reset()
    {
        _extractor = new SlideMlFragmentExtractor();
        _merger.Reset();
    }
}
