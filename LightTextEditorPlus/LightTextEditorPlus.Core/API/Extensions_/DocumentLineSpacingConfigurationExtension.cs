using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文本编辑控件文档行距配置辅助类
/// </summary>
public static class DocumentLineSpacingConfigurationExtension
{
    /// <summary>
    /// 使用 PPT 的行距样式
    /// </summary>
    /// <param name="textEditorCore"></param>
    public static void UsePptLineSpacingStyle(this TextEditorCore textEditorCore)
    {
        textEditorCore.LineSpacingConfiguration = new DocumentLineSpacingConfiguration()
        {
            LineSpacingAlgorithm = LineSpacingAlgorithm.PPT,
            LineSpacingStrategy = LineSpacingStrategy.FullExpand,
            VerticalCharInLineAlignment = RatioVerticalCharInLineAlignment.BottomAlignment,
        };
    }

    /// <summary>
    /// 使用 WPF 的行距样式
    /// </summary>
    /// <param name="textEditorCore"></param>
    public static void UseWpfLineSpacingStyle(this TextEditorCore textEditorCore)
    {
        textEditorCore.LineSpacingConfiguration = new DocumentLineSpacingConfiguration()
        {
            LineSpacingAlgorithm = LineSpacingAlgorithm.WPF,
            LineSpacingStrategy = LineSpacingStrategy.FirstLineShrink,
            VerticalCharInLineAlignment = RatioVerticalCharInLineAlignment.BottomAlignment,
        };
    }

    /// <summary>
    /// 使用 Word 的行距样式
    /// </summary>
    /// <param name="textEditorCore"></param>
    public static void UseWordLineSpacingStrategy(this TextEditorCore textEditorCore)
    {
        textEditorCore.LineSpacingConfiguration = new DocumentLineSpacingConfiguration()
        {
            LineSpacingAlgorithm = LineSpacingAlgorithm.WPF, // todo 还没实现 Word 的行距，先借用 WPF 的行距
            LineSpacingStrategy = LineSpacingStrategy.FullExpand,
            VerticalCharInLineAlignment = RatioVerticalCharInLineAlignment.TopAlignment,
        };
    }
}
