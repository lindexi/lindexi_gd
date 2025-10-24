using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Platform;
using LightTextEditorPlus.Primitive;

using SkiaSharp;

using System;
using LightTextEditorPlus.Core;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 字符属性。创建字符属性时，应该基于所在文本框的某个现有的 <see cref="SkiaTextRunProperty"/> 使用 <see langword="with"/> 关键字进行修改和创建新的属性
/// 正确做法应该是使用如下示例代码方式创建
/// <code>
/// SkiaTextRunProperty runProperty = textEditor.StyleRunProperty with
/// {
///     FontName = xxx,
///     FontSize = xxx,
///     Foreground = xxx,
/// };
/// </code>
/// </summary>
[APIConstraint("RunProperty.txt")]
public record SkiaTextRunProperty : LayoutOnlyRunProperty
{
    internal SkiaTextRunProperty(SkiaPlatformResourceManager skiaPlatformResourceManager)
    {
        ResourceManager = skiaPlatformResourceManager;
    }

    /// <inheritdoc />
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
    /// 是否没有找到合适的渲染字体，而使用了回滚字符
    /// </summary>
    internal bool IsMissRenderFont { get; init; } = false;

    /// <inheritdoc />
    public override bool IsInvalidRunProperty => IsMissRenderFont;

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

    /// <summary>
    /// 前景色
    /// </summary>
    public SkiaTextBrush Foreground { get; init; } = SkiaTextBrush.DefaultBlackSolidColorBrush;

    /// <summary>
    /// 背景色
    /// </summary>
    [Obsolete("还没完成背景色的支持", true)]
    // todo 支持背景色
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

    /// <summary>
    /// 文本的装饰集合
    /// </summary>
    public TextEditorImmutableDecorationCollection DecorationCollection { get; init; }

    /// <summary>
    /// 从文本编辑器的当前设置创建一个新的 <see cref="SkiaTextRunProperty"/>，应该基于所在文本框的某个现有的 <see cref="SkiaTextRunProperty"/> 使用 <see langword="with"/> 关键字进行修改和创建新的属性。本方法只是一个提示作用
    /// 正确做法应该是使用如下示例代码方式创建
    /// <code>
    /// SkiaTextRunProperty runProperty = textEditor.StyleRunProperty with
    /// {
    ///     FontName = xxx,
    ///     FontSize = xxx,
    ///     Foreground = xxx,
    /// };
    /// </code>
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public static SkiaTextRunProperty FromTextEditor(TextEditorCore textEditor)
    {
        return textEditor.DocumentManager.StyleRunProperty.AsSkiaRunProperty();
    }

#if USE_AllInOne && USE_AVALONIA
    /// <summary>
    /// 从文本编辑器的当前设置创建一个新的 <see cref="SkiaTextRunProperty"/>，应该基于所在文本框的某个现有的 <see cref="SkiaTextRunProperty"/> 使用 <see langword="with"/> 关键字进行修改和创建新的属性。本方法只是一个提示作用
    /// 正确做法应该是使用如下示例代码方式创建
    /// <code>
    /// SkiaTextRunProperty runProperty = textEditor.StyleRunProperty with
    /// {
    ///     FontName = xxx,
    ///     FontSize = xxx,
    ///     Foreground = xxx,
    /// };
    /// </code>
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public static SkiaTextRunProperty FromTextEditor(TextEditor textEditor)
    {
        return textEditor.StyleRunProperty;
    }
#endif

    private void InvalidateFont()
    {
        // 后续可以考虑删除，因为缓存策略是在每次布局的时候制作的
    }

    internal SKFontStyle ToSKFontStyle() =>
        new SKFontStyle(FontWeight, Stretch, FontStyle);

    /// <inheritdoc />
    public override bool Equals(IReadOnlyRunProperty? other)
    {
        if (other is SkiaTextRunProperty skiaTextRunProperty)
        {
            return Equals(skiaTextRunProperty);
        }

        return false;
    }

    /// <inheritdoc />
    public virtual bool Equals(SkiaTextRunProperty? other)
    {
        if (other is null) return false;

        if (!base.Equals(other))
        {
            return false;
        }

        if (!string.Equals(RenderFontName, other.RenderFontName))
        {
            return false;
        }

        if (Foreground != other.Foreground) //&& Background == other.Background 
        {
            return false;
        }

        if (Stretch != other.Stretch)
        {
            return false;
        }

        if (FontWeight != other.FontWeight)
        {
            return false;
        }

        if (FontStyle != other.FontStyle)
        {
            return false;
        }

        if (!Opacity.Equals(other.Opacity))
        {
            return false;
        }

        if (!DecorationCollection.Equals(other.DecorationCollection))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hashCode = new HashCode();
        hashCode.Add(RenderFontName);
        // 忽略 FontName
        hashCode.Add(FontSize);
        hashCode.Add(FontVariant);

        hashCode.Add(Foreground);
        hashCode.Add(Stretch);
        hashCode.Add(FontWeight);
        hashCode.Add(FontStyle);
        hashCode.Add(Opacity);
        hashCode.Add(DecorationCollection.GetHashCode());

        return hashCode.ToHashCode();
    }
}
