namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个 <see cref="IImmutableRun"/> 列表
/// </summary>
public interface IImmutableRunList
{
    /// <summary>
    /// 字符数量
    /// </summary>
    int CharCount { get; }

    /// <summary>
    /// 文本段的数量
    /// </summary>
    int RunCount { get; }

    /// <summary>
    /// 获取文本段
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    IImmutableRun GetRun(int index);
}