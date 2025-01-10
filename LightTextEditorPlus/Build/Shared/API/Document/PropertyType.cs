#if !USE_SKIA || USE_AllInOne
using System;
using System.ComponentModel;

namespace LightTextEditorPlus.Document;

/// <summary>
/// 属性类型
/// </summary>
public enum PropertyType
{
    /// <summary>
    /// 全部属性的更改
    /// </summary>
    All,

    /// <summary>
    /// 字号
    /// </summary>
    FontSize,

    /// <summary>
    /// 字体
    /// </summary>
    FontName = 2,

    /// <summary>
    /// 字体
    /// </summary>
    [Obsolete("为了有更好的字体回退策略，请使用 FontName 替代。", true)]
    FontFamily = 2,

    /// <summary>
    /// 前景色
    /// </summary>
    Foreground,

    /// <summary>
    /// 字体样式
    /// </summary>
    FontStyle,

    /// <summary>
    /// 加粗
    /// </summary>
    FontWeight,

    /// <summary>
    /// 斜体
    /// </summary>
    FontVariants,

    /// <summary>
    /// 下划线
    /// </summary>
    TextDecoration,

    /// <summary>
    /// 特效
    /// </summary>
    EffectsArgs,

    /// <summary>
    /// 透明度
    /// </summary>
    Opacity,

    /// <summary>
    /// 段前距离
    /// </summary>
    ParagraphBefore,

    /// <summary>
    /// 段后距离
    /// </summary>
    ParagraphAfter,

    /// <summary>
    /// 段落方向设置(从左向右、从右向左)
    /// </summary>
    Direction,

    /// <summary>
    /// 布局方式
    /// </summary>
    ArrangingType,

    /// <summary>
    /// 项目符号
    /// </summary>
    MarkerStyle,

    /// <summary>
    /// 行距
    /// </summary>
    LineSpacing,

    /// <summary>
    /// 缩进
    /// </summary>
    MarginLeft,

    /// <summary>
    /// 文本水平对齐
    /// </summary>
    TextHorizontalAlignment,

    /// <summary>
    /// 文本垂直对齐
    /// </summary>
    TextVerticalAlignment,

    /// <summary>
    /// 文本水平对齐
    /// </summary>
    [Obsolete("请使用 TextHorizontalAlignment 代替")] 
    [EditorBrowsable(EditorBrowsableState.Never)]
    TextAlignment = TextHorizontalAlignment,

    /// <summary>
    /// 下划线
    /// </summary>
    Hyperlink,

    /// <summary>
    /// 文本属性
    /// </summary>
    RunProperty,

    /// <summary>
    /// 段落属性
    /// </summary>
    ParagraphProperty,

    /// <summary>
    /// 文档属性
    /// </summary>
    DocumentProperty
}
#endif
