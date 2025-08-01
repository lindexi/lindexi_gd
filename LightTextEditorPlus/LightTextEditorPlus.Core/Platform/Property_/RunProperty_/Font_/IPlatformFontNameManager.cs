namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 平台相关的字体资源管理器
/// </summary>
public interface IPlatformFontNameManager
{
    /// <summary>
    /// 获取默认的字体名
    /// </summary>
    /// <returns></returns>
    string GetFallbackDefaultFontName();

    /// <summary>
    /// 检查字体是否安装
    /// </summary>
    /// <param name="fontName"></param>
    /// <returns></returns>
    bool CheckFontFamilyInstalled(string fontName);
}
