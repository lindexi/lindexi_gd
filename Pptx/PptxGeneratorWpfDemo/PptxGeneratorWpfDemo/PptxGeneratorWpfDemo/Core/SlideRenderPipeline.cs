using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// 渲染流水线外观类型，编排四阶段流水线：Parse → PreLayout → PreMeasure → FinalLayout → Render。
/// </summary>
public sealed class SlideRenderPipeline
{
    private readonly SlideMlParser _parser;
    private readonly ISlideLayoutEngine _layoutEngine;
    private readonly ISlideRenderEngine _renderEngine;
    private readonly SlidePipelineContext _context;

    /// <summary>
    /// 初始化 <see cref="SlideRenderPipeline"/> 的新实例，使用默认布局引擎和渲染引擎。
    /// </summary>
        public SlideRenderPipeline()
            : this(new SlideLayoutEngine(), new SlideRenderEngine())
        {
        }

        /// <summary>
        /// 初始化 <see cref="SlideRenderPipeline"/> 的新实例。
        /// </summary>
        /// <param name="layoutEngine">布局引擎。</param>
        /// <param name="renderEngine">渲染引擎。</param>
        internal SlideRenderPipeline(ISlideLayoutEngine layoutEngine, ISlideRenderEngine renderEngine)
            : this(layoutEngine, renderEngine, new SlidePipelineContext())
        {
        }

        /// <summary>
        /// 初始化 <see cref="SlideRenderPipeline"/> 的新实例并指定渲染上下文。
        /// </summary>
        /// <param name="layoutEngine">布局引擎。</param>
        /// <param name="renderEngine">渲染引擎。</param>
        /// <param name="context">渲染上下文。</param>
        internal SlideRenderPipeline(ISlideLayoutEngine layoutEngine, ISlideRenderEngine renderEngine, SlidePipelineContext context)
    {
        _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
        _renderEngine = renderEngine ?? throw new ArgumentNullException(nameof(renderEngine));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _parser = new SlideMlParser();
    }

    /// <summary>
    /// 获取渲染上下文。
    /// </summary>
    public SlidePipelineContext Context => _context;

    /// <summary>
    /// 将 SlideML 渲染为预览图，并返回回填后的 XML 与警告信息。
    /// 编排四阶段流水线：Parse → PreLayout → PreMeasure → FinalLayout → Render。
    /// </summary>
    /// <param name="slideXml">SlideML XML 字符串。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>渲染结果。</returns>
    public async Task<SlideRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            throw new ArgumentException("SlideML 不能为空。", nameof(slideXml));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var normalizedXml = SlideXmlUtilities.NormalizeXml(SlideXmlUtilities.ExtractXml(slideXml));

                        // 清空上一轮诊断，解析器直接写入 _context
                                    _context.Reset();
                                    var page = _parser.Parse(normalizedXml, _context);

            // 阶段 ①: PreLayout
            _layoutEngine.PreLayout(page, _context);

            // 阶段 ②: PreMeasure（在 UI 线程执行）
            SlideElementMeasurements measurements = null!;
            var previewBitmap = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                measurements = _renderEngine.PreMeasure(page, _context);

                // 阶段 ③: FinalLayout
                _layoutEngine.FinalLayout(page, _context, measurements);

                // 阶段 ④: Render
                var bitmap = new RenderTargetBitmap(_context.CanvasWidth, _context.CanvasHeight, 96.0, 96.0, PixelFormats.Pbgra32);
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    _renderEngine.Render(page, dc, _context);
                }
                bitmap.Render(visual);

                return bitmap;
            });

            var renderedXml = SlideXmlUtilities.FormatRenderedXml(normalizedXml, page, _context);
            return new SlideRenderResult
            {
                InputXml = normalizedXml,
                OutputXml = renderedXml,
                Warnings = _context.Warnings,
                Errors = _context.Errors,
                PreviewBitmap = previewBitmap,
            };
        }
        catch (Exception ex) when (ex is SlideMlParseException or System.Xml.XmlException)
        {
            var previewBitmap = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return _renderEngine.RenderErrorPreview(ex.Message, _context);
            });
            return new SlideRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Warnings = [$"[Warning] parser: SlideML 解析失败，{ex.Message}"],
                Errors = [],
                PreviewBitmap = previewBitmap,
            };
        }
    }
}
