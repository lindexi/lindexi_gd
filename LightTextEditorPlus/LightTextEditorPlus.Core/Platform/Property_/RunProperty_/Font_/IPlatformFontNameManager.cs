namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的字体资源管理器
/// 管理文本使用的字体名称和字体回退策略
/// </summary>
public interface IPlatformFontNameManager
{
    /// <summary>
    /// 获取默认的字体名
    /// </summary>
    /// <returns></returns>
    string GetFallbackDefaultFontName();

    /// <summary>
    /// 获取字体回退策略
    /// </summary>
    /// <param name="desiredFontName"></param>
    /// <returns></returns>
    string GetFallbackFontName(string desiredFontName);

    /// <summary>
    /// 检查字体是否安装
    /// </summary>
    /// <param name="fontName"></param>
    /// <returns></returns>
    bool CheckFontFamilyInstalled(string fontName);
}
