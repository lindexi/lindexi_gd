namespace LightTextEditorPlus.Core.Utils;

/// <summary>
/// 文本的上下文，放置暴露给外界的文本全局的静态上下文操作
/// </summary>
public static class TextContext
{
    /// <summary>
    /// 磅转像素的参数
    /// </summary>
    public const double PoundToPx = 1.333333333;

    /// <summary>
    /// 像素转磅的参数
    /// </summary>
    public const double PxToPound = 0.75;

    /// <summary>
    /// 支持的最大磅值字号
    /// </summary>
    public const double MaxFontSize = 500;

    /// <summary>
    /// 支持的最小磅值字号
    /// </summary>
    public const double MinFontSize = 1;

    /// <summary>
    /// 文本使用的阈值
    /// </summary>
    public const double Epsilon = 0.00001;

    /// <summary>
    /// 表示无法识别的文本字符
    /// </summary>
    public const char UnknownChar = '\uFFFD';

    /// <summary>
    /// 文本库统一写入的换行符，此换行符和平台无关，所有平台写入相同的值
    /// </summary>
    public const string NewLine = "\n";

    /// <summary>
    /// 表示一个无效字符
    /// </summary>
    internal const char NotChar = '\uFFFE';

    /// <summary>
    /// 默认用来测量的文本
    /// </summary>
    internal const string DefaultText = "1";

    /// <summary>
    /// 默认用来测量的文本
    /// </summary>
    public const char DefaultChar = '1';

    /// <summary>
    ///  文本内部渲染使用的double的阈值, 渲染宽度计算时应使用较大的精度
    /// </summary>
    internal const double RenderEpsilon = 0.001;
}