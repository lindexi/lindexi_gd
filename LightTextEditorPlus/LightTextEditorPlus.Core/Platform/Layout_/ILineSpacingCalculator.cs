using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 行距计算方法
/// </summary>
public interface ILineSpacingCalculator
{
    /// <summary>
    /// 计算行距
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    LineSpacingCalculateResult CalculateLineSpacing(in LineSpacingCalculateArgument argument);
}