using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.TestsFramework;

/// <summary>
/// 固定字符使用字号作为尺寸的测量
/// </summary>
/// 用于在单元测试无视具体平台和具体字体的影响
public class FixedCharSizeCharInfoMeasurer : ICharInfoMeasurer
{
    public FixedCharSizeCharInfoMeasurer(double baselineRatio = 4d / 5)
    {
        _baselineRatio = baselineRatio;
    }

    private readonly double _baselineRatio;

    public void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument)
    {
        CharData currentCharData = argument.CurrentCharData;

        double fontSize = currentCharData.RunProperty.FontSize;
        var size = new TextSize(fontSize, fontSize);
        // 设置基线为字号大小的向上一点点
        double baseline = fontSize * _baselineRatio;

        argument.CharDataLayoutInfoSetter.SetCharDataInfo(currentCharData, size, baseline);
    }
}
