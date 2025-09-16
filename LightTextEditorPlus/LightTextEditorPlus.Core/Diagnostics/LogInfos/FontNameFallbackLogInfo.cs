using System.Diagnostics;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 由于字体没有安装等情况，导致字体回滚的日志信息
/// </summary>
/// <param name="OriginFontName"></param>
/// <param name="FallbackInfo"></param>
public readonly record struct FontNameFallbackLogInfo(string OriginFontName, FontFallbackInfo FallbackInfo)
{
    /// <inheritdoc />
    public override string ToString()
    {
        if (FallbackInfo.IsFallback)
        {
            return $"从 '{OriginFontName}' 回滚到 '{FallbackInfo.FallbackFontName}' 字体";
        }
        else
        {
            Debug.Assert(FallbackInfo.IsFallbackFailed);
            return $"字体 '{OriginFontName}' 不存在，且没有找到可用的回滚字体，最终回滚到默认字体 '{FallbackInfo.FallbackFontName}'";
        }
    }
}