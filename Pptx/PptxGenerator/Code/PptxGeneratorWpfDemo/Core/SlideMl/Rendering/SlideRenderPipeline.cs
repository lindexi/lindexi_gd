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
/// 实现 <see cref="ISlideRenderPipeline"/> 接口。
/// </summary>
public sealed class SlideRenderPipeline : ISlideRenderPipeline
{
    private readonly SlideMlParser _parser;
    private readonly ISlideLayoutEngine _layoutEngine;
    private readonly ISlideRenderEngine _renderEngine;
    private readonly SlidePipelineContext _context;

    public SlideRenderPipeline()
        : this(new SlideLayoutEngine(), new SlideRenderEngine())
    {
    }

    internal SlideRenderPipeline(ISlideLayoutEngine layoutEngine, ISlideRenderEngine renderEngine)
        : this(layoutEngine, renderEngine, new SlidePipelineContext())
    {
    }

    internal SlideRenderPipeline(ISlideLayoutEngine layoutEngine, ISlideRenderEngine renderEngine, SlidePipelineContext context)
    {
        _layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
        _renderEngine = renderEngine ?? throw new ArgumentNullException(nameof(renderEngine));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _parser = new SlideMlParser();
    }

    public SlidePipelineContext Context => _context;

    /// <inheritdoc />
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
                PreviewImage = new WpfPreviewImage(previewBitmap),
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
                Warnings = new[] { $"[Warning] parser: SlideML 解析失败，{ex.Message}" },
                PreviewImage = new WpfPreviewImage(previewBitmap),
            };
        }
    }
}
