using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;
using PptxGenerator.Streaming;

namespace PptxGenerator.Pipeline;

/// <summary>
/// 流式生成的可变状态，跨重试轮次和跨轮对话复用。
/// 包含合并器 DOM 树、Id/StyleId 索引、资源管理器和渲染服务状态。
/// </summary>
internal sealed class SlideStreamingState
{
    /// <summary>
    /// 初始化 <see cref="SlideStreamingState"/> 的新实例。
    /// </summary>
    /// <param name="promptProvider">提示词提供者。</param>
    /// <param name="renderPipeline">SlideML 渲染管道。</param>
    /// <param name="dispatcher">主线程调度器。</param>
    public SlideStreamingState(
        ISlideMlPromptProvider promptProvider,
        ISlideMlRenderPipeline renderPipeline,
        IMainThreadDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(promptProvider);
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(dispatcher);

        Pipeline = new SlideStreamingPipeline(promptProvider, renderPipeline, dispatcher);
        Context = new SlideMlPipelineContext();
    }

    /// <summary>
    /// 流式渲染管道（包含 <see cref="SlideMlStreamingMerger"/>）。
    /// 跨轮复用，保留 DOM 树和 Id/StyleId 索引。
    /// </summary>
    public SlideStreamingPipeline Pipeline { get; }

    /// <summary>
    /// 渲染上下文（包含诊断信息和资源管理器）。
    /// 跨轮复用，每轮重试前调用 <see cref="SlideMlPipelineContext.Reset"/> 清空诊断信息。
    /// </summary>
    public SlideMlPipelineContext Context { get; }
}
