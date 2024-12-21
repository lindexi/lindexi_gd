using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Document;

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
            InvalidateFont();
        }
    }

    internal SkiaPlatformResourceManager ResourceManager { get; init; }

    internal RenderingRunPropertyInfo GetRenderingRunPropertyInfo(char unicodeChar = '1')
    {
        return ResourceManager.GetRenderingRunPropertyInfo(this, unicodeChar);
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
}
