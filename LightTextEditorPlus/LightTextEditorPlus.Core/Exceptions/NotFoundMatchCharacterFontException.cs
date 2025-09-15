using LightTextEditorPlus.Core.Primitive;

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
    public NotFoundMatchCharacterFontException(Utf32CodePoint unknownChar) : base($"无法找到 '{unknownChar.ToString()}' 字符的可渲染字体，可能当前设备未安装任何一款字体")
    {
        UnknownChar = unknownChar;
    }

    /// <summary>
    /// 表示无法渲染的字符
    /// </summary>
    public Utf32CodePoint UnknownChar { get; }
}