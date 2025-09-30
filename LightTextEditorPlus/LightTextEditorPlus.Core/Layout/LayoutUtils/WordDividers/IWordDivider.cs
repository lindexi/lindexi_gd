using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

/// <summary>
/// 单词分隔器，分词器
/// </summary>
public interface IWordDivider
{
    /// <summary>
    /// 分割单词。需要额外考虑连字符的情况
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    DivideWordResult DivideWord(in DivideWordArgument argument);

    /// <summary>
    /// 获取传入光标所在的单词选择范围
    /// </summary>
    /// <returns></returns>
    /// 选词需要分词算法，请参阅：
    /// [UWP WinRT 使用系统自带的分词库对字符串文本进行分词](https://blog.lindexi.com/post/UWP-WinRT-%E4%BD%BF%E7%94%A8%E7%B3%BB%E7%BB%9F%E8%87%AA%E5%B8%A6%E7%9A%84%E5%88%86%E8%AF%8D%E5%BA%93%E5%AF%B9%E5%AD%97%E7%AC%A6%E4%B8%B2%E6%96%87%E6%9C%AC%E8%BF%9B%E8%A1%8C%E5%88%86%E8%AF%8D.html )
    /// [dotnet 简单使用 ICU 库进行分词和分行 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18622917 )
    GetCaretWordResult GetCaretWord(in GetCaretWordArgument argument);
}