using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Platform;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 字符属性。创建字符属性时，应该基于所在文本框的某个现有的 <see cref="SkiaTextRunProperty"/> 使用 <see langword="with"/> 关键字进行修改和创建新的属性
/// </summary>
[APIConstraint("RunProperty.txt")]
public record SkiaTextRunProperty : LayoutOnlyRunProperty
{
    internal SkiaTextRunProperty(SkiaPlatformResourceManager skiaPlatformResourceManager)
    {
        ResourceManager = skiaPlatformResourceManager;
    }

    public override FontName FontName
    {
        get => base.FontName;
        init
        {
            if (value.Equals(base.FontName))
            {
                return;
            }

            base.FontName = value;

            _renderFontName = null;
        }
    }

    /// <summary>
    /// 获取渲染字体名称
    /// </summary>
    internal string RenderFontName
    {
        get => _renderFontName ?? FontName.UserFontName;
        init => _renderFontName = value;
    }

    private readonly string? _renderFontName;

    internal SkiaPlatformResourceManager ResourceManager { get; init; }

    internal RenderingRunPropertyInfo GetRenderingRunPropertyInfo()
    {
        return GetRenderingRunPropertyInfo(TextContext.DefaultCharCodePoint);
    }

    /// <summary>
    /// 获取字符的渲染属性
    /// </summary>
    /// <param name="codePointToDebug">只有调试意义的代码点</param>
    /// <returns></returns>
    internal RenderingRunPropertyInfo GetRenderingRunPropertyInfo(Utf32CodePoint codePointToDebug)
    {
        return ResourceManager.GetRenderingRunPropertyInfo(this, codePointToDebug);
    }

    /// <summary>
    /// 不透明度 [0-1] 范围
    /// </summary>
    public double Opacity { get; init; } = 1;
    public SKColor Foreground { get; init; } = SKColors.Black;
    public SKColor Background { get; init; } = SKColors.Transparent;

    /// <summary>
    /// 获取描述与某个字体与该字体的正常纵横比相比的拉伸程度
    /// </summary>
    public SKFontStyleWidth Stretch
    {
        get => _stretch;
        init
        {
            _stretch = value;
            InvalidateFont();
        }
    }

    private readonly SKFontStyleWidth _stretch = SKFontStyleWidth.Normal;

    /// <summary>
    /// 字的粗细度，默认值为Normal
    /// </summary>
    /// 粗细表：
    /// 100	Thin (Hairline)
    /// 200	Extra Light (Ultra Light)
    /// 300	Light
    /// 400	Normal
    /// 500	Medium
    /// 600	Semi Bold (Demi Bold)
    /// 700	Bold
    /// 800	Extra Bold (Ultra Bold)
    /// 900	Black (Heavy)
    public SKFontStyleWeight FontWeight
    {
        get => _fontWeight;
        init
        {
            _fontWeight = value;
            InvalidateFont();
        }
    }

    private readonly SKFontStyleWeight _fontWeight = SKFontStyleWeight.Normal;

    /// <summary>
    /// 斜体
    /// </summary>
    public SKFontStyleSlant FontStyle
    {
        get => _fontStyle;
        init
        {
            _fontStyle = value;
            InvalidateFont();
        }
    }

    private readonly SKFontStyleSlant _fontStyle = SKFontStyleSlant.Upright;

    private void InvalidateFont()
    {
        // 后续可以考虑删除，因为缓存策略是在每次布局的时候制作的
    }

    internal SKFontStyle ToSKFontStyle() =>
        new SKFontStyle(FontWeight, Stretch, FontStyle);
}
