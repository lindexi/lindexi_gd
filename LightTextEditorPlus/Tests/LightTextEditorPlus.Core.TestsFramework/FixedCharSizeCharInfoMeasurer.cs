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
    public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
    {
        return new CharInfoMeasureResult(new TextRect(0, 0, charInfo.RunProperty.FontSize, charInfo.RunProperty.FontSize));
    }
}