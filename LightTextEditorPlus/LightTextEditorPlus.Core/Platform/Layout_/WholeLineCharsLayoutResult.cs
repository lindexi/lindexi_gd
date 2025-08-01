using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 布局一行的结果
/// </summary>
/// <param name="CurrentLineCharTextSize">当前的字符布局尺寸</param>
/// <param name="WholeTakeCount">所取的字符数量</param>
public readonly record struct WholeLineCharsLayoutResult(TextSize CurrentLineCharTextSize, int WholeTakeCount);