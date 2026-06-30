using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

using System;
using System.Collections.Generic;

namespace PptxGenerator.Streaming;

/// <summary>
/// 流式渲染服务，封装 <see cref="ISlideMlRenderPipeline"/>，提供带节流的实时渲染和最终强制渲染。
/// </summary>
public sealed class SlideStreamRenderService
{
    private readonly ISlideMlRenderPipeline _renderPipeline;
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly TimeSpan _minRenderInterval;
    private DateTimeOffset _lastRenderTime;

    /// <summary>
    /// 初始化 <see cref="SlideStreamRenderService"/> 的新实例。
    /// </summary>
    /// <param name="renderPipeline">SlideML 渲染管道。</param>
    /// <param name="dispatcher">主线程调度器。</param>
    /// <param name="minRenderInterval">最小渲染间隔，默认为 500 毫秒。</param>
    public SlideStreamRenderService(
        ISlideMlRenderPipeline renderPipeline,
        IMainThreadDispatcher dispatcher,
        TimeSpan? minRenderInterval = null)
    {
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _renderPipeline = renderPipeline;
        _dispatcher = dispatcher;
        _minRenderInterval = minRenderInterval ?? TimeSpan.FromMilliseconds(500);
    }

    /// <summary>
    /// 渲染完成后触发，携带最新的渲染结果。
    /// </summary>
    public event Action<SlideStreamRenderResult>? Rendered;

    /// <summary>
    /// 当前预览图。
    /// </summary>
    public IPreviewImage? CurrentPreviewImage { get; private set; }

    /// <summary>
    /// 当前渲染后的 XML。
    /// </summary>
    public string CurrentRenderedXml { get; private set; } = string.Empty;

    /// <summary>
    /// 当前渲染警告。
    /// </summary>
    public IReadOnlyList<string> CurrentWarnings { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// 尝试渲染（带节流）。距上次渲染不足 <see cref="_minRenderInterval"/> 时跳过。
    /// </summary>
    /// <param name="slideXml">当前合并后的 XML。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否实际执行了渲染。</returns>
    public async Task<bool> TryRenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slideXml);

        if (string.IsNullOrWhiteSpace(slideXml))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (_lastRenderTime != default && now - _lastRenderTime < _minRenderInterval)
        {
            return false;
        }

        await RenderCoreAsync(slideXml, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 强制渲染（忽略节流），用于流结束后的最终渲染。
    /// </summary>
    /// <param name="slideXml">当前合并后的 XML。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task FinalRenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slideXml);

        if (string.IsNullOrWhiteSpace(slideXml))
        {
            return;
        }

        await RenderCoreAsync(slideXml, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 核心渲染逻辑，调度到主线程执行。
    /// </summary>
    private async Task<SlideMlRenderResult> RenderCoreAsync(string slideXml, CancellationToken cancellationToken)
    {
        _lastRenderTime = DateTimeOffset.UtcNow;

        var renderResult = await _renderPipeline.RenderAsync(slideXml, cancellationToken).ConfigureAwait(false);

        var result = new SlideStreamRenderResult
        {
            InputXml = renderResult.InputXml,
            OutputXml = renderResult.OutputXml,
            Warnings = renderResult.Warnings,
            Errors = renderResult.Errors,
            PreviewImage = renderResult.PreviewImage,
        };

        await _dispatcher.InvokeAsync(() =>
        {
            CurrentPreviewImage = result.PreviewImage;
            CurrentRenderedXml = result.OutputXml;
            CurrentWarnings = result.Warnings;
            Rendered?.Invoke(result);
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        return renderResult;
    }
}
