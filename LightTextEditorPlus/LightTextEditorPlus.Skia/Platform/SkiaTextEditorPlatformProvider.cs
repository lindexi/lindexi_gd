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
        // 兼容获取的方法
        // 详细请参阅 cb389420514f0eb9cdb6ed79378e5e4508c2e2c4
        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.AsSkiaRunProperty().GetRenderingRunPropertyInfo();
        SKFont skFont = renderingRunPropertyInfo.Font;
        return (-skFont.Metrics.Ascent + skFont.Metrics.Descent) / skFont.Size;
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