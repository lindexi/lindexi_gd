namespace LightTextEditorPlus.Core.Utils.Patterns;

/// <summary>
/// 表示匹配的接口
/// </summary>
public interface IPattern
{
    /// <summary>
    /// 是否输入的字符在范围内
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    bool IsInRange(char c);

    /// <summary>
    /// 确定字符串中包含范围内字符，对空白字符串返回不符合
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    bool ContainInRange(string text);

    /// <summary>
    /// 是否输入的字符串每个字符都在范围内，对空白字符串返回符合
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    bool AreAllInRange(string text);
}