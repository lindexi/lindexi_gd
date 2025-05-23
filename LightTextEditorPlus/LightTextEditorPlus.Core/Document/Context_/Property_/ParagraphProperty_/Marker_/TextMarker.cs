﻿namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 项目符号
/// </summary>
public abstract class TextMarker
{
    /// <summary>
    /// 是否跟随段落首个字符的样式（颜色、大小）
    /// </summary>
    public bool ShouldFollowParagraphFirstCharRunProperty { get; init; }

    /// <summary>
    /// 最小的缩进值
    /// </summary>
    /// 在 Word 里面是有带最小缩进值的功能的，按照标尺的方式缩进
    public double? MinimumIndent { get; init; }

    /// <summary>
    /// 项目符号的字符属性
    /// </summary>
    public IReadOnlyRunProperty? RunProperty { get; init; }

    /// <summary>
    /// 用于限制程序集外继承的科技，确保一定只有两个类型，分别是有编码和无符号
    /// </summary>
    internal abstract void DisableInherit();
}