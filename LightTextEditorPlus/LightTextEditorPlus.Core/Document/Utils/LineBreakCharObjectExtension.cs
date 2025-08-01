namespace LightTextEditorPlus.Core.Document.Utils;

/// <summary>
/// 换行字符对象扩展
/// </summary>
public static class LineBreakCharObjectExtension
{
    /// <summary>
    /// 判断是否是换行字符
    /// </summary>
    /// <param name="charObject"></param>
    /// <returns></returns>
    public static bool IsLineBreak(this ICharObject charObject)
    {
        return ReferenceEquals(charObject, LineBreakCharObject.Instance);
    }
}