using AgentLib;
using PptxGenerator.Models;

namespace PptxGenerator.Rendering;

/// <summary>
/// UI 框架无关的 SlideML 渲染流水线实现。
/// 编排四阶段流水线：Parse → PreLayout → PreMeasure → FinalLayout → Render。
/// 通过 <see cref="IMainThreadDispatcher"/> 抽象 UI 线程调度，消除对特定 UI 框架的依赖。
/// </summary>
public sealed class SlideMlRenderPipeline : ISlideMlRenderPipeline
{
    private readonly SlideMlParser _parser;
    private readonly ISlideMlLayoutEngine _layoutEngine;
    private readonly ISlideMlRenderEngine _renderEngine;
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly SlideMlPipelineContext _context;

    /// <summary>
    /// 初始化 <see cref="SlideMlRenderPipeline"/> 的新实例。
    /// </summary>
    /// <param name="layoutEngine">布局引擎。</param>
    /// <param name="renderEngine">渲染引擎。</param>
    /// <param name="dispatcher">主线程调度器，用于在 UI 线程执行测量与渲染。</param>
    /// <param name="context">可选的管道上下文；为 <see langword="null"/> 时将创建新实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="layoutEngine"/>、<paramref name="renderEngine"/> 或 <paramref name="dispatcher"/> 为 <see langword="null"/>。</exception>
    public SlideMlRenderPipeline(
        ISlideMlLayoutEngine layoutEngine,
        ISlideMlRenderEngine renderEngine,
        IMainThreadDispatcher dispatcher,
        SlideMlPipelineContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(layoutEngine);
        ArgumentNullException.ThrowIfNull(renderEngine);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _layoutEngine = layoutEngine;
        _renderEngine = renderEngine;
        _dispatcher = dispatcher;
        _context = context ?? new SlideMlPipelineContext();
        _parser = new SlideMlParser();
    }

    /// <summary>
    /// 获取当前管道上下文，包含解析/排版/渲染过程中的警告与错误信息。
    /// </summary>
    public SlideMlPipelineContext Context => _context;

    /// <inheritdoc />
    public async Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            throw new ArgumentException("SlideML 不能为空。", nameof(slideXml));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var normalizedXml = SlideMlXmlUtilities.NormalizeXml(SlideMlXmlUtilities.ExtractXml(slideXml));

            _context.Reset();
            var page = _parser.Parse(normalizedXml, _context);

            // 阶段 ①: PreLayout
            _layoutEngine.PreLayout(page, _context);

            // 阶段 ② + ③ + ④: PreMeasure → FinalLayout → Render（在 UI 线程执行）
            SlideMlElementMeasurements measurements = null!;
            var previewImage = await _dispatcher.InvokeAsync(async () =>
            {
                measurements = _renderEngine.PreMeasure(page, _context);
                _layoutEngine.FinalLayout(page, _context, measurements);
                return _renderEngine.Render(page, _context);
            }).ConfigureAwait(false);

            var renderedXml = SlideMlXmlUtilities.FormatRenderedXml(normalizedXml, page, _context);
            return new SlideMlRenderResult
            {
                InputXml = normalizedXml,
                OutputXml = renderedXml,
                Warnings = _context.Warnings,
                Errors = _context.Errors,
                PreviewImage = previewImage,
            };
        }
        catch (Exception ex) when (ex is SlideMlParseException or System.Xml.XmlException)
        {
            var previewImage = await _dispatcher.InvokeAsync(async () =>
            {
                return _renderEngine.RenderErrorPreview(ex.Message, _context);
            }).ConfigureAwait(false);
            return new SlideMlRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Warnings = new[] { $"[Warning] parser: SlideML 解析失败，{ex.Message}" },
                PreviewImage = previewImage,
            };
        }
    }
}
