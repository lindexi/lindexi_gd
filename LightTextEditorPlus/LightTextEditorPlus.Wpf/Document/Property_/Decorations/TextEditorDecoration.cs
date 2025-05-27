using System;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 文本的装饰
/// </summary>
public abstract class TextEditorDecoration
{
    /// <summary>
    /// 文本的装饰
    /// </summary>
    protected TextEditorDecoration(TextDecorationLocation textDecorationLocation)
    {
        TextDecorationLocation = textDecorationLocation;
    }

    /// <summary>
    /// 获取文本的装饰放在文本的哪里
    /// </summary>
    public TextDecorationLocation TextDecorationLocation { get; }

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
    /// 隐式转换
    /// </summary>
    public static implicit operator TextEditorDecoration(TextDecoration textDecoration)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 构建装饰的参数
/// </summary>
/// <param name="RunProperty"></param>
/// <param name="CharDataList">当前相同属性的字符列表，从准备绘制的起始字符开始</param>
/// <param name="RecommendedBounds">推荐的渲染范围</param>
/// <param name="LineRenderInfo">段落内的行的渲染信息</param>
///// <param name="CurrentCharIndexInLine">当前准备绘制的起始字符所在当前行的坐标</param>
public readonly record struct BuildDecorationArgument(RunProperty RunProperty, /*int CurrentCharIndexInLine,*/ TextReadOnlyListSpan<CharData> CharDataList, TextRect RecommendedBounds, ParagraphLineRenderInfo LineRenderInfo, TextEditor TextEditor);

/// <summary>
/// 构建装饰的结果
/// </summary>
/// <param name="Drawing">构建结果内容</param>
/// <param name="TakeCharCount">装饰层用到了多少个字符参与构建。接下来下一次调用渲染就会跳过这些字符。比如下划线可以整一片一起，那就可以快速跳过一片相同属性的字符了</param>
public readonly record struct BuildDecorationResult(Drawing? Drawing, int TakeCharCount)
{
}

