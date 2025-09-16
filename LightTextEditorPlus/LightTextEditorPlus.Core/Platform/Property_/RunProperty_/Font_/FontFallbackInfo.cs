namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 字体回退信息
/// </summary>
/// <param name="FallbackFontName">字体名</param>
/// <param name="IsFallback">这个字体是否是回退的。False: 字体本身不需要回退</param>
/// <param name="IsFallbackFailed">是否回退失败</param>
public readonly record struct FontFallbackInfo(string FallbackFontName, bool IsFallback, bool IsFallbackFailed);