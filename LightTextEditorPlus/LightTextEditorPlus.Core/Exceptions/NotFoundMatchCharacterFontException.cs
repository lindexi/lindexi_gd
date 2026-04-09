using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 没有找到可匹配字符渲染的字体异常
/// </summary>
public class NotFoundMatchCharacterFontException : TextEditorException
{
    /// <summary>
    /// 创建没有找到可匹配字符渲染的字体异常
    /// </summary>
    /// <param name="unknownChar"></param>
    public NotFoundMatchCharacterFontException(Utf32CodePoint unknownChar)
        : base(ExceptionMessages.Format(nameof(NotFoundMatchCharacterFontException) + "_Message", unknownChar))
    {
        UnknownChar = unknownChar;
    }

    /// <summary>
    /// 表示无法渲染的字符
    /// </summary>
    public Utf32CodePoint UnknownChar { get; }
}