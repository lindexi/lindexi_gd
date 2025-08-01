namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 管理文本使用的字体名称和字体回退策略。
/// </summary>
public interface IFontNameManager
{
    /// <summary>
    /// 获取字体回退策略
    /// </summary>
    /// <param name="desiredFontName"></param>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    string GetFallbackFontName(string desiredFontName, TextEditorCore textEditor);
}
