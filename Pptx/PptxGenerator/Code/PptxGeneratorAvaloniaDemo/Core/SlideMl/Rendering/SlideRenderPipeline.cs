using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace PptxGenerator;

/// <summary>
/// Avalonia 渲染流水线实现，编排四阶段流水线：
/// Parse → PreLayout → PreMeasure → FinalLayout → Render。
/// 实现 <see cref="ISlideRenderPipeline"/> 接口。
/// </summary>
public sealed class SlideRenderPipeline : ISlideRenderPipeline
{
    private readonly SlideMlParser _parser;
    private readonly ISlideLayoutEngine _layoutEngine;
    private readonly IAvaloniaSlideRenderEngine _renderEngine;
    private readonly SlidePipelineContext _context;

    public SlideRenderPipeline()
        : this(new SlideLayoutEngine(), new AvaloniaSlideRenderEngine())
    {
    }

    internal SlideRenderPipeline(ISlideLayoutEngine layoutEngine, IAvaloniaSlideRenderEngine renderEngine)
        : this(layoutEngine, renderEngine, new SlidePipelineContext())
    {
    }

    internal SlideRenderPipeline(ISlideLayoutEngine layoutEngine, IAvaloniaSlideRenderEngine renderEngine, SlidePipelineContext context)
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

            // 阶段 ② + ③ + ④: PreMeasure → FinalLayout → Render（在 UI 线程执行）
            SlideElementMeasurements measurements = null!;
            var previewBitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                measurements = _renderEngine.PreMeasure(page, _context);
                _layoutEngine.FinalLayout(page, _context, measurements);

                var bitmap = new RenderTargetBitmap(new PixelSize(_context.CanvasWidth, _context.CanvasHeight), new Vector(96, 96));
                using (var dc = bitmap.CreateDrawingContext())
                {
                    _renderEngine.Render(page, dc, _context);
                }

                return bitmap;
            });

            var renderedXml = SlideXmlUtilities.FormatRenderedXml(normalizedXml, page, _context);
            return new SlideRenderResult
            {
                InputXml = normalizedXml,
                OutputXml = renderedXml,
                Warnings = _context.Warnings,
                Errors = _context.Errors,
                PreviewImage = new AvaloniaPreviewImage(previewBitmap),
            };
        }
        catch (Exception ex) when (ex is SlideMlParseException or System.Xml.XmlException)
        {
            var previewBitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                return _renderEngine.RenderErrorPreview(ex.Message, _context);
            });
            return new SlideRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Warnings = new[] { $"[Warning] parser: SlideML 解析失败，{ex.Message}" },
                PreviewImage = new AvaloniaPreviewImage(previewBitmap),
            };
        }
    }
}