namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本点格式化器
/// </summary>
public static class TextPointFormatter
{
    /// <summary>
    /// 转换为数学点格式，如 (1.23,4.56)
    /// </summary>
    /// <param name="textPoint"></param>
    /// <returns></returns>
    public static string ToMathPointFormat(this TextPoint textPoint)
        => $"({textPoint.X:#.##},{textPoint.Y:#.##})";
}