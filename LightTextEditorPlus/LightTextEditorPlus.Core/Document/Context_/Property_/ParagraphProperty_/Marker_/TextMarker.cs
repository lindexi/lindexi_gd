using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Document;

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
    /// 项目符号的字符属性
    /// </summary>
    public IReadOnlyRunProperty? RunProperty { get; init; }

    /// <summary>
    /// 用于限制程序集外继承的科技，确保一定只有两个类型，分别是有编码和无符号
    /// </summary>
    internal abstract void DisableInherit();
}