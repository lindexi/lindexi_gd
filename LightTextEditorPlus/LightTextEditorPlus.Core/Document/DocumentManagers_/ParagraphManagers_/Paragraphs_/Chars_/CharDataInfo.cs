using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符固有信息。字符固有信息必定是和具体的字符属性相关的，即包括字符、字体、字号等属性相关联
/// </summary>
public readonly record struct CharDataInfo
{
    /// <summary>
    /// 字符固有信息。字符固有信息必定是和具体的字符属性相关的，即包括字符、字体、字号等属性相关联
    /// </summary>
    /// <param name="frameSize">FrameSize 尺寸，即字外框尺寸。文字外框尺寸</param>
    /// <param name="faceSize">Character Face Size 字面尺寸，字墨尺寸，字墨大小，字墨量。文字的字身框中，字图实际分布的空间的尺寸。小于等于 <see cref="FrameSize"/> 尺寸</param>
    /// <param name="baseline">基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的</param>
    public CharDataInfo(TextSize frameSize, TextSize faceSize, double baseline)
    {
        FrameSize = frameSize;
        FaceSize = faceSize;
        Baseline = baseline;
        Status = CharDataInfoStatus.Normal;
    }

    /// <summary>
    /// 渲染字符在对应的字体的字形索引
    /// </summary>
    /// OpenType® Specification Version 1.9.1 https://learn.microsoft.com/en-us/typography/opentype/spec/
    /// [字体65,535的限制什么时候能修正？ - 知乎](https://www.zhihu.com/question/526308089 )
    /// 规范中规定了 GlyphIndex 是 UInt16 类型，因此最大值为 65535 个
    /// 对于无效的字符，GlyphIndex 为 0
    /// 不必担心字体里面没有包含、或者无法将 0 的 GlyphIndex 字符识别为错误的字形。在规范里面已经定义了 0 为 .notdef 字形，即 □ 方框符号
    /// 参阅：
    /// - Skia 系实验： https://github.com/lindexi/lindexi_gd/tree/e8bb42f77302527286179c8877d903465436ba18/SkiaSharp/HaberekeljallcahaiJanayadaynur
    /// - WPF DX 系文档： https://learn.microsoft.com/en-us/windows/win32/api/dwrite/nf-dwrite-idwritefontface-getglyphindices
    /// - OpenType 规范： https://learn.microsoft.com/en-us/typography/opentype/otspec170/recom#shape-of-notdef-glyph
    /// > When characters are not present in the font this method returns the index 0, which is the undefined glyph or ".notdef" glyph.
    /// OpenType 字体建议（OpenType 1.7）：
    /// First Four Glyphs in Fonts. TrueType outline fonts should have the following four glyphs at the glyph ID indicated.
    /// TrueType 轮廓字体应在指定的字形 ID 处包含以下四个字形
    /// - Glyph ID=0 , Glyph name=".notdef" , Unicode value=undefined ,  Description="undefined glyph" 未定义字形
    /// - Glyph ID=1 , Glyph name=".null" , Unicode value=null(U+0000) , Description="null glyph" 空字形
    /// - Glyph ID=2 , Glyph name="CR" , Unicode value=null(U+000D) , Description="non-marking return" 非标记回车
    /// - Glyph ID=3 , Glyph name="space" , Unicode value=space(U+0020) , Description="space" 空格
    /// 尽管写的是建议，但是实际上绝大多数字体都遵循了这个规范
    public required ushort GlyphIndex { get; init; }

    /// <summary>
    /// 表示未定义字形
    /// </summary>
    public const ushort UndefinedGlyphIndex = 0;

    /// <summary>
    /// 字符固有信息的状态
    /// </summary>
    public CharDataInfoStatus Status { get; init; }

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
        Baseline = double.NaN,
        Status = CharDataInfoStatus.Invalid,
        GlyphIndex = 0,
    };

    /// <summary>
    /// 是否无效的字符信息
    /// </summary>
    public bool IsInvalid => FrameSize.IsInvalid || FaceSize.IsInvalid || double.IsNaN(Baseline) || Status == CharDataInfoStatus.Invalid;

    /// <summary>FrameSize 尺寸，即字外框尺寸。文字外框尺寸</summary>
    public TextSize FrameSize { get; init; }

    /// <summary>Character Face Size 字面尺寸，字墨尺寸，字墨大小，字墨量。文字的字身框中，字图实际分布的空间的尺寸。小于等于 <see cref="FrameSize"/> 尺寸</summary>
    public TextSize FaceSize { get; init; }

    /// <summary>基线，相对于字符的左上角，字符坐标系。即无论这个字符放在哪一行哪一段，这个字符的基线都是一样的</summary>
    public double Baseline { get; init; }
}

/// <summary>
/// 字符固有信息的状态
/// </summary>
public enum CharDataInfoStatus : byte
{
    /// <summary>
    /// 无效的
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// 正常
    /// </summary>
    Normal,

    /// <summary>
    /// 没有从字体里面获取到定义的信息
    /// </summary>
    Undefined,

    /// <summary>
    /// 连写字的起始字符
    /// </summary>
    LigatureStart,

    /// <summary>
    /// 连写字的后续字符
    /// </summary>
    LigatureContinue,
}