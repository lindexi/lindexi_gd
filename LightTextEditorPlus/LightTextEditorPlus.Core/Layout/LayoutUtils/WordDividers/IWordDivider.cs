namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

/// <summary>
/// 单词分隔器，分词器
/// </summary>
public interface IWordDivider
{
    /// <summary>
    /// 分割单词
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    DivideWordResult DivideWord(in DivideWordArgument argument);
}