namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本库使用的厚度，各项边距
/// </summary>
public readonly record struct TextThickness(double Left, double Top, double Right, double Bottom);