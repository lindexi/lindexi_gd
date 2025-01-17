using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文本的行距
/// </summary>
public static class TextLineSpacings
{
    /// <summary>
    /// 单倍行距，默认值
    /// </summary>
    /// <returns></returns>
    public static ITextLineSpacing SingleLineSpace()
    {
        return SingleTextLineSpace;
    }

    private static readonly MultipleTextLineSpace SingleTextLineSpace = new(1);

    /// <summary>
    /// 多倍行距
    /// </summary>
    /// <param name="lineSpacing">行距倍数</param>
    /// <returns></returns>
    public static ITextLineSpacing MultipleLineSpace(double lineSpacing)
    {
        return new MultipleTextLineSpace(lineSpacing);
    }

    /// <summary>
    /// 固定行距
    /// </summary>
    /// <param name="lineSpacing">单位与字号相同，为具体框架的单位</param>
    /// <returns></returns>
    public static ITextLineSpacing ExactlyLineSpace(double lineSpacing)
    {
        return new ExactlyTextLineSpace(lineSpacing);
    }

    /// <inheritdoc cref="ExactlyLineSpace"/>
    /// 决定采用 Exactly 这个词代替 Fixed 的原因请看 <see cref="ExactlyTextLineSpace"/> 的注释
    [Obsolete("请使用 ExactlyLineSpace 方法。此方法的存在仅仅只是为了告诉你，更加正确的是调用 ExactlyLineSpace 方法而已")]
    public static ITextLineSpacing FixedLineSpacing(double lineSpacing) => ExactlyLineSpace(lineSpacing);

    /// <summary>
    /// 最小的行距。如果当前文本行距小于指定的行距，则使用指定的行距。否则则使用当前行的字高
    /// </summary>
    /// <param name="lineSpacing">固定值</param>
    /// <returns></returns>
    /// 直接可以解决如 Javanese Text 或 Myanmar Text 字体的问题
    /// <exception cref="NotImplementedException"></exception>
    internal static ITextLineSpacing AtLeastLineSpace(double lineSpacing)
    {
        // [What reasons make different Line spacing between "exact" and - Microsoft Community](https://answers.microsoft.com/en-us/msoffice/forum/all/what-reasons-make-different-line-spacing-between/ca1267f9-0cfe-4f26-8048-bbd7a6a46398 )
        throw new NotImplementedException($"还没实现此功能");
    }
}
