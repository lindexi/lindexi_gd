using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Document;
using SkiaSharp;

namespace LightTextEditorPlus.Platform;

/// <summary>
/// Skia 文本编辑器平台提供者
/// </summary>
public class SkiaTextEditorPlatformProvider : PlatformProvider
{
    /// <summary>
    /// 文本编辑器
    /// </summary>
    public SkiaTextEditor TextEditor { get; internal set; }
    // 框架确保赋值
        = null!;

    /// <summary>
    /// 从传入字符信息获取字体的行距信息
    /// </summary>
    /// <param name="runProperty"></param>
    /// <returns></returns>
    public override double GetFontLineSpacing(IReadOnlyRunProperty runProperty)
    {
        // 兼容获取行距的方法
        // 从 Skia 里面拿到的属性含义是不正确的，如 [dotnet 简单聊聊 Skia 里的 SKFontMetrics 的各项属性作用 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18621674 )
        // 重新按照 DirectWrite 的 [DWRITE_FONT_METRICS 文档](https://learn.microsoft.com/en-us/windows/win32/api/dwrite/ns-dwrite-dwrite_font_metrics) 进行计算
        // > LineGap: The line gap in font design units. Recommended additional white space to add between lines to improve legibility. The recommended line spacing (baseline-to-baseline distance) is the sum of ascent, descent, and lineGap. The line gap is usually positive or zero but can be negative, in which case the recommended line spacing is less than the height of the character alignment box.
        // 可以知道，其数值上应该是 
        // FontMetrics fontMetrics = xx;
        // var lineSpacing = (fontMetrics.Ascent + fontMetrics.Descent + fontMetrics.LineGap) / (double) fontMetrics.DesignUnitsPerEm;
        // 在 Skia 里面，不存在 LineGap 的概念，但与其数值相同的是 Leading 属性，只好取来计算
        // 详细请参阅 684937f058c75134f7af925bb88350b25149bc39
        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.AsSkiaRunProperty().GetRenderingRunPropertyInfo();
        SKFont skFont = renderingRunPropertyInfo.Font;
        return (-skFont.Metrics.Ascent + skFont.Metrics.Descent + skFont.Metrics.Leading) / skFont.Size;
    }

    private SkiaPlatformResourceManager? _skiaPlatformFontManager;

    private SkiaPlatformRunPropertyCreator? _skiaPlatformRunPropertyCreator;

    /// <summary>
    /// 获取资源管理器
    /// </summary>
    /// <returns></returns>
    protected virtual SkiaPlatformResourceManager GetSkiaPlatformResourceManager()
    {
        return _skiaPlatformFontManager ??= new SkiaPlatformResourceManager(TextEditor);
    }

    /// <inheritdoc />
    public override IPlatformFontNameManager GetPlatformFontNameManager()
    {
        return GetSkiaPlatformResourceManager();
    }

    /// <inheritdoc />
    public override IPlatformRunPropertyCreator GetPlatformRunPropertyCreator()
    {
        return _skiaPlatformRunPropertyCreator ??= new SkiaPlatformRunPropertyCreator(GetSkiaPlatformResourceManager(), TextEditor);
    }

    /// <inheritdoc />
    public override IRenderManager GetRenderManager()
    {
        return TextEditor;
    }

    //public override ISingleCharInLineLayouter GetSingleRunLineLayouter()
    //{
    // // 原本以为 Skia 可以通过 BreakText 进行一行布局，然而过程发现其没有带语言文化，且即使带了估计也不符合预期。因此废弃此类型，换成 SkiaCharInfoMeasurer 只测量字符尺寸。但后续可能依然需要 HarfBuzz 辅助处理合写字的情况，到时候也许依然需要开放此类型。只不过这个过程中不需要再次测量字符尺寸而已
    //    return new SkiaSingleCharInLineLayouter(TextEditor);
    //}

    //public override IWholeLineCharsLayouter GetWholeLineCharsLayouter()
    //{
    //    return _skiaWholeLineLayouter ??= new SkiaWholeLineCharsLayouter();
    //}

    //private SkiaWholeLineCharsLayouter? _skiaWholeLineLayouter;

    /// <inheritdoc />
    public override ICharInfoMeasurer GetCharInfoMeasurer()
    {
        return _charInfoMeasurer ??= new SkiaCharInfoMeasurer();
    }

    private SkiaCharInfoMeasurer? _charInfoMeasurer;
}