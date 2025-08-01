namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本尺寸格式化器
/// </summary>
public static class TextSizeFormatter
{
    /// <summary>
    /// 使用逗号分割方式，输出为 `Width,Height` 格式
    /// </summary>
    /// <param name="textSize"></param>
    /// <returns></returns>
    public static string ToCommaSplitWidthAndHeight(this TextSize textSize)
        => $"{textSize.Width:#.##},{textSize.Height:#.##}";

    internal static string ToDebugText(this TextSize textSize)
    {
        if (textSize == TextSize.Invalid)
        {
            return $"W:{textSize.Width} H:{textSize.Height} {nameof(TextSize.Invalid)}";
        }
        else
        {
            return $"W:{textSize.Width} H:{textSize.Height}";
        }
    }
}