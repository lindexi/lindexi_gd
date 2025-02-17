namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的厚度，各项边距
/// </summary>
public readonly record struct TextThickness(double Left, double Top, double Right, double Bottom)
{
    /// <summary>
    /// 表示无效的厚度边距
    /// </summary>
    public static TextThickness Invalid => new TextThickness(double.NegativeInfinity, double.NegativeInfinity,
        double.NegativeInfinity, double.NegativeInfinity);
}