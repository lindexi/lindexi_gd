#if DirectTextEditorDefinition

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 预设的文本的装饰
/// </summary>
public static class TextEditorDecorations
{
    /// <summary>
    /// 下划线
    /// </summary>
    public static TextEditorDecoration Underline => UnderlineTextEditorDecoration.Instance;

    /// <summary>
    /// 删除线
    /// </summary>
    public static TextEditorDecoration Strikethrough => StrikethroughTextEditorDecoration.Instance;

    /// <summary>
    /// 着重号
    /// </summary>
    public static TextEditorDecoration EmphasisDots => EmphasisDotsTextEditorDecoration.Instance;

#if USE_WPF
    /// <summary>
    /// 波浪线
    /// </summary>
    public static TextEditorDecoration WaveLine => WaveLineTextEditorDecoration.Instance;
#endif
}
#endif