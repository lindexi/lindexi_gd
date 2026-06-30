using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Pipeline;
using PptxGenerator.Prompt;
using PptxGenerator.Rendering;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PptxGenerator.Streaming;

/// <summary>
/// 流式渲染编排管道，将片段提取器、合并器和渲染服务串联起来。
/// 从 LLM 流式输出接收增量文本，提取完整 XML 片段，合并到 DOM 树，每合并一个片段后立即尝试实时渲染（带节流）。
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
    /// <param name="minRenderInterval">最小渲染间隔，默认为 500 毫秒。传 <see langword="null"/> 使用默认值。</param>
    public SlideStreamingPipeline(
        ISlideMlPromptProvider promptProvider,
        ISlideMlRenderPipeline renderPipeline,
        IMainThreadDispatcher dispatcher,
        TimeSpan? minRenderInterval = null)
    {
        ArgumentNullException.ThrowIfNull(promptProvider);
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _promptProvider = promptProvider;
        _dispatcher = dispatcher;
        _merger = new SlideMlStreamingMerger();
        _renderService = new SlideStreamRenderService(renderPipeline, dispatcher, minRenderInterval);
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
    /// 处理 LLM 流式增量文本。提取完整片段后合并到 DOM 树，并在每个片段合并成功后立即尝试实时渲染。
    /// 合并出错的片段会被自动回滚，防止错误状态污染后续合并。
    /// </summary>
    /// <param name="text">增量文本。</param>
    /// <param name="context">渲染上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ProcessIncrementalTextAsync(string text, SlideMlPipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(context);

        _extractor.Append(text);
        var fragments = _extractor.TryExtractFragments();

        foreach (var fragment in fragments)
        {
            var errorCountBefore = context.Errors.Count;
            _merger.AcceptFragment(fragment, context);
            FragmentReceived?.Invoke(fragment);

            // 片段合并产生错误时回滚到合并前状态，防止错误片段污染 DOM 树
            if (context.Errors.Count > errorCountBefore)
            {
                _merger.RollbackLastVersion();
                continue;
            }

            // 每个片段合并成功后立即尝试实时渲染（带节流）
            var mergedXml = _merger.GetMergedXml();
            if (!string.IsNullOrWhiteSpace(mergedXml))
            {
                await _renderService.TryRenderAsync(mergedXml, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// 处理流结束，强制最终渲染（忽略节流）。
    /// 如果流结束时产生错误，自动回滚到最后一个干净版本。
    /// </summary>
    /// <param name="context">渲染上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ProcessStreamEndAsync(SlideMlPipelineContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var errorCountBefore = context.Errors.Count;

        // 处理缓冲区中剩余的内容（容错）
        var remaining = _extractor.GetRemaining();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            context.AddWarning($"[Warning] 流结束时缓冲区有未完成的内容: {remaining}");
        }

        // 流结束时如果有新错误，回滚到最后一个干净版本
        if (context.Errors.Count > errorCountBefore)
        {
            _merger.RollbackLastVersion();
        }

        var mergedXml = _merger.GetMergedXml();
        if (!string.IsNullOrWhiteSpace(mergedXml))
        {
            await _renderService.FinalRenderAsync(mergedXml, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// 仅重置片段提取器缓冲区，保留合并器 DOM 树和 Id/StyleId 索引状态。
    /// 用于错误重试时清理上一轮残留的缓冲区内容，同时保留已成功合并的片段。
    /// </summary>
    public void ResetExtractor()
    {
        _extractor = new SlideMlFragmentExtractor();
    }
}
