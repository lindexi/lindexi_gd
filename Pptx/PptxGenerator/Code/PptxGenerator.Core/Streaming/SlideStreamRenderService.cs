using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Streaming;

/// <summary>
/// 流式渲染服务，封装 <see cref="ISlideMlRenderPipeline"/> 并通过 <see cref="IMainThreadDispatcher"/> 确保线程安全。
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
    public SlideStreamRenderService(ISlideMlRenderPipeline renderPipeline, IMainThreadDispatcher dispatcher)
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
    /// 渲染指定的 SlideML XML。
    /// </summary>
    /// <param name="slideXml">SlideML XML 字符串。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RenderAsync(string slideXml, CancellationToken cancellationToken = default)
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
    }
}
