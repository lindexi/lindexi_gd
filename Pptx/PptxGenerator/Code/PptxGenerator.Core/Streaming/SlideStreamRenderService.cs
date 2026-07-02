using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

using System;
using System.Collections.Generic;

namespace PptxGenerator.Streaming;

/// <summary>
/// 流式渲染服务，封装 <see cref="ISlideMlRenderPipeline"/>，提供即时渲染以检测错误。
/// </summary>
public sealed class SlideStreamRenderService
{
    private readonly ISlideMlRenderPipeline _renderPipeline;
    private readonly IMainThreadDispatcher _dispatcher;

    /// <summary>
    /// 初始化 <see cref="SlideStreamRenderService"/> 的新实例。
    /// </summary>
    /// <param name="renderPipeline">SlideML 渲染管道。</param>
    /// <param name="dispatcher">主线程调度器。</param>
    public SlideStreamRenderService(
        ISlideMlRenderPipeline renderPipeline,
        IMainThreadDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _renderPipeline = renderPipeline;
        _dispatcher = dispatcher;
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
    /// 渲染指定 XML，用于片段合并后即时检测渲染错误或流结束后的最终渲染。
    /// </summary>
    /// <param name="slideXml">当前合并后的 XML。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>渲染结果；XML 为空时返回 <see langword="null"/>。</returns>
    public async Task<SlideStreamRenderResult?> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slideXml);

        if (string.IsNullOrWhiteSpace(slideXml))
        {
            return null;
        }

        return await RenderCoreAsync(slideXml, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 核心渲染逻辑，调度到主线程执行。
    /// </summary>
    private async Task<SlideStreamRenderResult> RenderCoreAsync(string slideXml, CancellationToken cancellationToken)
    {
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

        return result;
    }
}
