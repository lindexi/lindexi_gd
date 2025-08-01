namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 扩展方法，提供一些文本字符属性的扩展方法
/// </summary>
public static class RunPropertyExtension
{
    /// <summary>
    /// 获取考虑了上下标之后的渲染字体大小
    /// </summary>
    /// <param name="runProperty"></param>
    /// <returns></returns>
    public static double GetRenderFontSize(this IReadOnlyRunProperty runProperty)
    {
        if (runProperty.FontVariant.IsNormal)
        {
            return runProperty.FontSize;
        }
        else
        {
            // 上下标的字号减半
            // 在 PPT 里面，取的是 2/3 的字号大小，而不是 1/2 的字号大小
            // 在当前文本库算法里面，上下标是 1/2 的字号。而 PPT 里面是 2/3 的字号。想要对齐 PPT 的行为，就需要进行以下计算 `字号/ 1/2 * 2/3`
            return runProperty.FontSize / 2;
        }
    }
}