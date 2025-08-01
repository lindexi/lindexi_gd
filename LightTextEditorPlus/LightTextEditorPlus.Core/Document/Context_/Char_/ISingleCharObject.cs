namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个由单个字符构成的字符
/// </summary>
/// 由于文本里面存在一些语言是不能使用一个 char 表示一个字符的。所以大部分情况下都是需要使用 string 表示
public interface ISingleCharObject : ICharObject
{
    /// <summary>
    /// 获取字符
    /// </summary>
    /// <returns></returns>
    char GetChar();
}