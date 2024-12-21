namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文本字符创建器
/// </summary>
internal static class TextCharObjectCreator
{
    /// <summary>
    /// 根据传入的字符串创建字符对象
    /// </summary>
    /// <param name="text">传入的字符串</param>
    /// <param name="charIndex">字符的起始点</param>
    /// <param name="charCount">表示一个字符所使用的 char 数量。对于一些 表情 符号，需要两个 char 才能表示</param>
    /// <returns></returns>
    public static ICharObject CreateCharObject(string text, int charIndex, int charCount = 1)
    {
        if (charCount == 1)
        {
            return new SingleCharObject(text[charIndex]);
        }
        else
        {
            return new TextSpanCharObject(text, charIndex, charCount);
        }
    }
}