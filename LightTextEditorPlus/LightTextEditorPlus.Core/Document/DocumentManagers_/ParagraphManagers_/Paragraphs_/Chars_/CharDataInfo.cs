using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符固有信息
/// </summary>
/// <param name="FrameSize">FrameSize 尺寸，即字外框尺寸。文字外框尺寸</param>
/// <param name="FaceSize">Character Face Size 字面尺寸，字墨尺寸，字墨大小，字墨量。文字的字身框中，字图实际分布的空间的尺寸。小于等于 <see cref="FrameSize"/> 尺寸</param>
/// <param name="Baseline">基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的</param>
public readonly record struct CharDataInfo(TextSize FrameSize, TextSize FaceSize, double Baseline)
{
     

    /// <summary>
    /// 字符的尺寸。字符意义上的字符尺寸。等同于 <see cref="FrameSize"/> 的值
    /// </summary>
    public TextSize Size => FrameSize;

    /// <summary>
    /// 表示无效的字符固有信息
    /// </summary>
    public static CharDataInfo Invalid => new CharDataInfo()
    {
        FrameSize = TextSize.Invalid,
        FaceSize = TextSize.Invalid,
        Baseline = double.NaN
    };

    /// <summary>
    /// 是否无效的字符信息
    /// </summary>
    public bool IsInvalid => FrameSize.IsInvalid || FaceSize.IsInvalid || double.IsNaN(Baseline);
}