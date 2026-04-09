#if DirectTextEditorDefinition
using System;
using System.Linq;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Decorations;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;

#if USE_WPF
using Editor = LightTextEditorPlus.TextEditor;
using System.Windows;
#elif USE_SKIA
using RunProperty = LightTextEditorPlus.Document.SkiaTextRunProperty;
using Editor = LightTextEditorPlus.SkiaTextEditor;
#endif

namespace LightTextEditorPlus.Document.Decorations;

// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global

/// <summary>
/// 文本的装饰
/// </summary>
public abstract class TextEditorDecoration : ITextEditorDecoration
{
    /// <summary>
    /// 文本的装饰
    /// </summary>
    protected TextEditorDecoration(TextEditorDecorationLocation textDecorationLocation)
    {
        TextDecorationLocation = textDecorationLocation;
    }

    /// <summary>
    /// 获取文本的装饰放在文本的哪里
    /// </summary>
    public TextEditorDecorationLocation TextDecorationLocation { get; }

    /// <summary>
    /// 创建装饰
    /// </summary>
    /// <returns></returns>
    public abstract BuildDecorationResult BuildDecoration(in BuildDecorationArgument argument);

    /// <summary>
    /// 从此装饰层中的视角认为两个 RunProperty 是否是相同的
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public virtual bool AreSameRunProperty(RunProperty a, RunProperty b)
    {
        return a.Equals(b);
    }

    /// <summary>
    /// 判断两个 RunProperty 是否是相同的，通过判断是否包含了当前装饰层的装饰来判断是否相同
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    protected bool CheckSameRunPropertyByContainsCurrentDecoration(RunProperty a, RunProperty b)
    {
        var aContains = a.DecorationCollection.Contains(this);
        var bContains = b.DecorationCollection.Contains(this);
        return aContains && bContains; // 为什么取都包含？因为首次判定，必然是包含当前的装饰，否则也就不会进来了
    }

#if USE_WPF
    /// <summary>
    /// 隐式转换
    /// </summary>
    public static implicit operator TextEditorDecoration(TextDecoration textDecoration)
    {
        if (TextDecorations.Underline[0] == textDecoration)
        {
            return TextEditorDecorations.Underline;
        }

        if (TextDecorations.Strikethrough[0] == textDecoration)
        {
            return TextEditorDecorations.Strikethrough;
        }

        throw new NotSupportedException();
    }

#endif

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        // 相等判断是通过是否相同的类型进行判断的
        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is not TextEditorDecoration other)
        {
            return false;
        }

        return other.GetType() == this.GetType() && TextDecorationLocation == other.TextDecorationLocation;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(TextDecorationLocation, GetType());
    }

    /// <summary>
    /// 获取推荐的范围边界
    /// </summary>
    /// <param name="location"></param>
    /// <param name="currentCharDataList"></param>
    /// <param name="lineRenderInfo"></param>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public static TextRect GetDecorationLocationRecommendedBounds
    (
        TextEditorDecorationLocation location,
        in TextReadOnlyListSpan<CharData> currentCharDataList,
        in ParagraphLineRenderInfo lineRenderInfo,
        Editor textEditor
    )
    {
        if (!textEditor.TextEditorCore.ArrangingType.IsHorizontal)
        {
            // 现在还不能支持竖排
            throw new NotSupportedException("竖排的文本装饰暂时不支持");
        }

        CharData maxFontSizeCharData = CharDataLayoutHelper.GetMaxFontSizeCharData(in currentCharDataList);
        // 经验值，大概就是 0.1-0.05 之间
        var ratio = 0.06;
        var height = maxFontSizeCharData.RunProperty.FontSize * ratio;
        // 这里是在没有 Kern 的情况下，刚好就是各个字符之和就等于宽度。如果有 Kern 的情况，则不正确
        // todo 后续加上 Kern 需要考虑这里的宽度情况
        var width = currentCharDataList.Sum(charData => charData.Size.Width);
        CharData firstCharData = currentCharDataList[0];
        var x = firstCharData.GetStartPoint().X;
        double y;

        if (location == TextEditorDecorationLocation.Underline)
        {
            y = firstCharData.GetBounds().Bottom - height;
        }
        else if (location == TextEditorDecorationLocation.Strikethrough)
        {
            TextRect bounds = maxFontSizeCharData.GetBounds();
            y = bounds.Top + (bounds.Height - height) / 2; // 在中间
        }
        else
        {
            throw new NotSupportedException("暂不支持");
        }

#if USE_WPF
        // 在 WPF 里面文本是按照行渲染的，如此可以获得更多的缓存实现逻辑
        // 而是 GetBounds 等获取的是文本框坐标系的，需要将其转换为行坐标系下
        y -= lineRenderInfo.Argument.StartPoint.Y;
#endif

        return new TextRect(x, y, width, height);
    }
}
#endif