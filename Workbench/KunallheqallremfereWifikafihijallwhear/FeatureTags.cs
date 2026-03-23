using System;
using System.Collections.Generic;
using System.Text;

// Copy from https://github.com/dotnet/wpf \src\Microsoft.DotNet.Wpf\src\PresentationCore\MS\Internal\FontFace\Tags.cs
namespace MS.Internal;

internal enum FeatureTags
{
    /// <summary>
    /// 访问所有替代字形；Access all alternates.
    /// </summary>
    AccessAllAlternates = 0x61616c74, // 'aalt'

    /// <summary>
    /// 基线上方字形形式；Above-base forms.
    /// </summary>
    AboveBaseForms = 0x61627666, // 'abvf'

    /// <summary>
    /// 基线上方标记定位；Above-base mark positioning.
    /// </summary>
    AboveBaseMarkPositioning = 0x6162766d, // 'abvm'

    /// <summary>
    /// 基线上方替换；Above-base substitutions.
    /// </summary>
    AboveBaseSubstitutions = 0x61627673, // 'abvs'

    /// <summary>
    /// 替代分数字形；Alternative fractions.
    /// </summary>
    AlternativeFractions = 0x61667263, // 'afrc'

    /// <summary>
    /// 阿坎德连字形式；Akhands forms.
    /// </summary>
    Akhands = 0x616b686e, // 'akhn'

    /// <summary>
    /// 基线下方字形形式；Below-base forms.
    /// </summary>
    BelowBaseForms = 0x626c7766, // 'blwf'

    /// <summary>
    /// 基线下方标记定位；Below-base mark positioning.
    /// </summary>
    BelowBaseMarkPositioning = 0x626c776d, // 'blwm'

    /// <summary>
    /// 基线下方替换；Below-base substitutions.
    /// </summary>
    BelowBaseSubstitutions = 0x626c7773, // 'blws'

    /// <summary>
    /// 从大写生成小型大写字母；Petite capitals from capitals.
    /// </summary>
    PetiteCapitalsFromCapitals = 0x63327063, // 'c2pc'

    /// <summary>
    /// 从大写生成小型大写；Small capitals from capitals.
    /// </summary>
    SmallCapitalsFromCapitals = 0x63327363, // 'c2sc'

    /// <summary>
    /// 上下文替代；Contextual alternates.
    /// </summary>
    ContextualAlternates = 0x63616c74, // 'calt'

    /// <summary>
    /// 区分大小写形式；Case-sensitive forms.
    /// </summary>
    CaseSensitiveForms = 0x63617365, // 'case'

    /// <summary>
    /// 字形组合与分解；Glyph composition and decomposition.
    /// </summary>
    GlyphCompositionDecomposition = 0x63636d70, // 'ccmp'

    /// <summary>
    /// ro 之后的连写形式；Conjunct form after ro.
    /// </summary>
    Conjunctformafterro = 0x63666172, // 'cfar'

    /// <summary>
    /// 上下文连字；Contextual ligatures.
    /// </summary>
    ContextualLigatures = 0x636c6967, // 'clig'

    /// <summary>
    /// 连写字形；Conjuncts.
    /// </summary>
    Conjuncts = 0x636a6374, // 'cjct'

    /// <summary>
    /// 大写字母间距；Capital spacing.
    /// </summary>
    CapitalSpacing = 0x63707370, // 'cpsp'

    /// <summary>
    /// 上下文花饰；Contextual swash.
    /// </summary>
    ContextualSwash = 0x63737768, // 'cswh'

    /// <summary>
    /// 草书连接定位；Cursive positioning.
    /// </summary>
    CursivePositioning = 0x63757273, // 'curs'

    /// <summary>
    /// 默认处理；Default processing.
    /// </summary>
    DefaultProcessing = 0x64666c74, // 'dflt'

    /// <summary>
    /// 距离调整；Distances.
    /// </summary>
    Distances = 0x64697374, // 'dist'

    /// <summary>
    /// 自选连字；Discretionary ligatures.
    /// </summary>
    DiscretionaryLigatures = 0x646c6967, // 'dlig'

    /// <summary>
    /// 分母字形；Denominators.
    /// </summary>
    Denominators = 0x646e6f6d, // 'dnom'

    /// <summary>
    /// 双元音字形；Diphthongs.
    /// </summary>
    Diphthongs = 0x64706e67, // 'dpng'

    /// <summary>
    /// 专家字形形式；Expert forms.
    /// </summary>
    ExpertForms = 0x65787074, // 'expt'

    /// <summary>
    /// 末尾字形替代；Final glyph alternates.
    /// </summary>
    FinalglyphAlternates = 0x66616c74, // 'falt'

    /// <summary>
    /// 终结形式；Terminal forms.
    /// </summary>
    TerminalForms = 0x66696e61, // 'fina'

    /// <summary>
    /// 第二终结形式；Terminal forms 2.
    /// </summary>
    TerminalForms2 = 0x66696e32, // 'fin2'

    /// <summary>
    /// 第三终结形式；Terminal forms 3.
    /// </summary>
    TerminalForms3 = 0x66696e33, // 'fin3'

    /// <summary>
    /// 分数字形；Fractions.
    /// </summary>
    Fractions = 0x66726163, // 'frac'

    /// <summary>
    /// 全宽字形；Full width.
    /// </summary>
    FullWidth = 0x66776964, // 'fwid'

    /// <summary>
    /// 半形式；Half forms.
    /// </summary>
    HalfForms = 0x68616c66, // 'half'

    /// <summary>
    /// Halant 形式；Halant forms.
    /// </summary>
    HalantForms = 0x68616c6e, // 'haln'

    /// <summary>
    /// 替代半宽；Alternate half width.
    /// </summary>
    AlternateHalfWidth = 0x68616c74, // 'halt'

    /// <summary>
    /// 历史形式；Historical forms.
    /// </summary>
    HistoricalForms = 0x68697374, // 'hist'

    /// <summary>
    /// 横排假名替代；Horizontal kana alternates.
    /// </summary>
    HorizontalKanaAlternates = 0x686b6e61, // 'hkna'

    /// <summary>
    /// 历史连字；Historical ligatures.
    /// </summary>
    HistoricalLigatures = 0x686c6967, // 'hlig'

    /// <summary>
    /// 韩文字形；Hangul.
    /// </summary>
    Hangul = 0x686e676c, // 'hngl'

    /// <summary>
    /// 半宽字形；Half width.
    /// </summary>
    HalfWidth = 0x68776964, // 'hwid'

    /// <summary>
    /// 补助康熙汉字形式；Hojo Kanji forms.
    /// </summary>
    HojoKanjiForms = 0x686f6a6f, // 'hojo'

    /// <summary>
    /// 初始形式；Initial forms.
    /// </summary>
    InitialForms = 0x696e6974, // 'init'

    /// <summary>
    /// 独立形式；Isolated forms.
    /// </summary>
    IsolatedForms = 0x69736f6c, // 'isol'

    /// <summary>
    /// 斜体形式；Italics.
    /// </summary>
    Italics = 0x6974616c, // 'ital'

    /// <summary>
    /// 日文形式；Japanese forms.
    /// </summary>
    JapaneseForms = 0x6a616a70, // 'jajp'

    /// <summary>
    /// 两端对齐替代；Justification alternatives.
    /// </summary>
    JustificationAlternatives = 0x6a616c74, // 'jalt'

    /// <summary>
    /// JIS 2004 形式；JIS 2004 forms.
    /// </summary>
    JIS04Forms = 0x6a703034, // 'jp04'

    /// <summary>
    /// JIS 1978 形式；JIS 1978 forms.
    /// </summary>
    JIS78Forms = 0x6a703738, // 'jp78'

    /// <summary>
    /// JIS 1983 形式；JIS 1983 forms.
    /// </summary>
    JIS83Forms = 0x6a703833, // 'jp83'

    /// <summary>
    /// JIS 1990 形式；JIS 1990 forms.
    /// </summary>
    JIS90Forms = 0x6a703930, // 'jp90'

    /// <summary>
    /// 字距调整；Kerning.
    /// </summary>
    Kerning = 0x6b65726e, // 'kern'

    /// <summary>
    /// 左边界；Left bounds.
    /// </summary>
    LeftBounds = 0x6c666264, // 'lfbd'

    /// <summary>
    /// 标准连字；Standard ligatures.
    /// </summary>
    StandardLigatures = 0x6c696761, // 'liga'

    /// <summary>
    /// 前导 Jamo 形式；Leading Jamo forms.
    /// </summary>
    LeadingJamoForms = 0x6c6a6d6f, // 'ljmo'

    /// <summary>
    /// 等高数字；Lining figures.
    /// </summary>
    LiningFigures = 0x6c6e756d, // 'lnum'

    /// <summary>
    /// 本地化形式；Localized forms.
    /// </summary>
    LocalizedForms = 0x6c6f636c, // 'locl'

    /// <summary>
    /// 标记定位；Mark positioning.
    /// </summary>
    MarkPositioning = 0x6d61726b, // 'mark'

    /// <summary>
    /// 中间形式；Medial forms.
    /// </summary>
    MedialForms = 0x6d656469, // 'medi'

    /// <summary>
    /// 第二中间形式；Medial forms 2.
    /// </summary>
    MedialForms2 = 0x6d656432, // 'med2'

    /// <summary>
    /// 数学希腊字母；Mathematical Greek.
    /// </summary>
    MathematicalGreek = 0x6d67726b, // 'mgrk'

    /// <summary>
    /// 标记到标记定位；Mark-to-mark positioning.
    /// </summary>
    MarktoMarkPositioning = 0x6d6b6d6b, // 'mkmk'

    /// <summary>
    /// 通过替换进行标记定位；Mark positioning via substitution.
    /// </summary>
    MarkPositioningviaSubstitution = 0x6d736574, // 'mset'

    /// <summary>
    /// 替代注释形式；Alternate annotation forms.
    /// </summary>
    AlternateAnnotationForms = 0x6e616c74, // 'nalt'

    /// <summary>
    /// NLC 汉字形式；NLC Kanji forms.
    /// </summary>
    NLCKanjiForms = 0x6e6c636b, // 'nlck'

    /// <summary>
    /// Nukta 形式；Nukta forms.
    /// </summary>
    NuktaForms = 0x6e756b74, // 'nukt'

    /// <summary>
    /// 分子字形；Numerators.
    /// </summary>
    Numerators = 0x6e756d72, // 'numr'

    /// <summary>
    /// 旧式数字；Old-style figures.
    /// </summary>
    OldStyleFigures = 0x6f6e756d, // 'onum'

    /// <summary>
    /// 光学边界；Optical bounds.
    /// </summary>
    OpticalBounds = 0x6f706264, // 'opbd'

    /// <summary>
    /// 序数字形；Ordinals.
    /// </summary>
    Ordinals = 0x6f72646e, // 'ordn'

    /// <summary>
    /// 装饰字形；Ornaments.
    /// </summary>
    Ornaments = 0x6f726e6d, // 'ornm'

    /// <summary>
    /// 比例替代宽度；Proportional alternate width.
    /// </summary>
    ProportionalAlternateWidth = 0x70616c74, // 'palt'

    /// <summary>
    /// 小型大写字母；Petite capitals.
    /// </summary>
    PetiteCapitals = 0x70636170, // 'pcap'

    /// <summary>
    /// 比例数字；Proportional figures.
    /// </summary>
    ProportionalFigures = 0x706e756d, // 'pnum'

    /// <summary>
    /// 前置基字形式；Pre-base forms.
    /// </summary>
    PrebaseForms = 0x70726566, // 'pref'

    /// <summary>
    /// 前置基字替换；Pre-base substitutions.
    /// </summary>
    PrebaseSubstitutions = 0x70726573, // 'pres'

    /// <summary>
    /// 后置基字形式；Post-base forms.
    /// </summary>
    PostbaseForms = 0x70737466, // 'pstf'

    /// <summary>
    /// 后置基字替换；Post-base substitutions.
    /// </summary>
    PostbaseSubstitutions = 0x70737473, // 'psts'

    /// <summary>
    /// 比例宽度；Proportional widths.
    /// </summary>
    ProportionalWidths = 0x70776964, // 'pwid'

    /// <summary>
    /// 四分之一宽度；Quarter widths.
    /// </summary>
    QuarterWidths = 0x71776964, // 'qwid'

    /// <summary>
    /// 随机化替代；Randomize.
    /// </summary>
    Randomize = 0x72616e64, // 'rand'

    /// <summary>
    /// Rakar 形式；Rakar forms.
    /// </summary>
    RakarForms = 0x726b7266, // 'rkrf'

    /// <summary>
    /// 必需连字；Required ligatures.
    /// </summary>
    RequiredLigatures = 0x726c6967, // 'rlig'

    /// <summary>
    /// Reph 形式；Reph form.
    /// </summary>
    RephForm = 0x72706866, // 'rphf'

    /// <summary>
    /// 右边界；Right bounds.
    /// </summary>
    RightBounds = 0x72746264, // 'rtbd'

    /// <summary>
    /// 从右到左替代；Right-to-left alternates.
    /// </summary>
    RightToLeftAlternates = 0x72746c61, // 'rtla'

    /// <summary>
    /// 注音 Ruby 形式；Ruby notation forms.
    /// </summary>
    RubyNotationForms = 0x72756279, // 'ruby'

    /// <summary>
    /// 风格替代；Stylistic alternates.
    /// </summary>
    StylisticAlternates = 0x73616c74, // 'salt'

    /// <summary>
    /// 科学下标；Scientific inferiors.
    /// </summary>
    ScientificInferiors = 0x73696e66, // 'sinf'

    /// <summary>
    /// 光学字号；Optical size.
    /// </summary>
    OpticalSize = 0x73697a65, // 'size'

    /// <summary>
    /// 小型大写；Small capitals.
    /// </summary>
    SmallCapitals = 0x736d6370, // 'smcp'

    /// <summary>
    /// 简化字形；Simplified forms.
    /// </summary>
    SimplifiedForms = 0x736d706c, // 'smpl'

    /// <summary>
    /// 风格集 1；Stylistic set 1.
    /// </summary>
    StylisticSet1 = 0x73733031, // 'ss01'

    /// <summary>
    /// 风格集 2；Stylistic set 2.
    /// </summary>
    StylisticSet2 = 0x73733032, // 'ss02'

    /// <summary>
    /// 风格集 3；Stylistic set 3.
    /// </summary>
    StylisticSet3 = 0x73733033, // 'ss03'

    /// <summary>
    /// 风格集 4；Stylistic set 4.
    /// </summary>
    StylisticSet4 = 0x73733034, // 'ss04'

    /// <summary>
    /// 风格集 5；Stylistic set 5.
    /// </summary>
    StylisticSet5 = 0x73733035, // 'ss05'

    /// <summary>
    /// 风格集 6；Stylistic set 6.
    /// </summary>
    StylisticSet6 = 0x73733036, // 'ss06'

    /// <summary>
    /// 风格集 7；Stylistic set 7.
    /// </summary>
    StylisticSet7 = 0x73733037, // 'ss07'

    /// <summary>
    /// 风格集 8；Stylistic set 8.
    /// </summary>
    StylisticSet8 = 0x73733038, // 'ss08'

    /// <summary>
    /// 风格集 9；Stylistic set 9.
    /// </summary>
    StylisticSet9 = 0x73733039, // 'ss09'

    /// <summary>
    /// 风格集 10；Stylistic set 10.
    /// </summary>
    StylisticSet10 = 0x73733130, // 'ss10'

    /// <summary>
    /// 风格集 11；Stylistic set 11.
    /// </summary>
    StylisticSet11 = 0x73733131, // 'ss11'

    /// <summary>
    /// 风格集 12；Stylistic set 12.
    /// </summary>
    StylisticSet12 = 0x73733132, // 'ss12'

    /// <summary>
    /// 风格集 13；Stylistic set 13.
    /// </summary>
    StylisticSet13 = 0x73733133, // 'ss13'

    /// <summary>
    /// 风格集 14；Stylistic set 14.
    /// </summary>
    StylisticSet14 = 0x73733134, // 'ss14'

    /// <summary>
    /// 风格集 15；Stylistic set 15.
    /// </summary>
    StylisticSet15 = 0x73733135, // 'ss15'

    /// <summary>
    /// 风格集 16；Stylistic set 16.
    /// </summary>
    StylisticSet16 = 0x73733136, // 'ss16'

    /// <summary>
    /// 风格集 17；Stylistic set 17.
    /// </summary>
    StylisticSet17 = 0x73733137, // 'ss17'

    /// <summary>
    /// 风格集 18；Stylistic set 18.
    /// </summary>
    StylisticSet18 = 0x73733138, // 'ss18'

    /// <summary>
    /// 风格集 19；Stylistic set 19.
    /// </summary>
    StylisticSet19 = 0x73733139, // 'ss19'

    /// <summary>
    /// 风格集 20；Stylistic set 20.
    /// </summary>
    StylisticSet20 = 0x73733230, // 'ss20'

    /// <summary>
    /// 下标字形；Subscript.
    /// </summary>
    Subscript = 0x73756273, // 'subs'

    /// <summary>
    /// 上标字形；Superscript.
    /// </summary>
    Superscript = 0x73757073, // 'sups'

    /// <summary>
    /// 花饰字形；Swash.
    /// </summary>
    Swash = 0x73777368, // 'swsh'

    /// <summary>
    /// 标题字形；Titling.
    /// </summary>
    Titling = 0x7469746c, // 'titl'

    /// <summary>
    /// 尾随 Jamo 形式；Trailing Jamo forms.
    /// </summary>
    TrailingJamoForms = 0x746a6d6f, // 'tjmo'

    /// <summary>
    /// 传统名称形式；Traditional name forms.
    /// </summary>
    TraditionalNameForms = 0x746e616d, // 'tnam'

    /// <summary>
    /// 表格数字；Tabular figures.
    /// </summary>
    TabularFigures = 0x746e756d, // 'tnum'

    /// <summary>
    /// 传统字形；Traditional forms.
    /// </summary>
    TraditionalForms = 0x74726164, // 'trad'

    /// <summary>
    /// 三分之一宽度；Third widths.
    /// </summary>
    ThirdWidths = 0x74776964, // 'twid'

    /// <summary>
    /// 单一大小写形式；Unicase.
    /// </summary>
    Unicase = 0x756e6963, // 'unic'

    /// <summary>
    /// 替代竖排度量；Alternate vertical metrics.
    /// </summary>
    AlternateVerticalMetrics = 0x76616c74, // 'valt'

    /// <summary>
    /// Vattu 变体；Vattu variants.
    /// </summary>
    VattuVariants = 0x76617475, // 'vatu'

    /// <summary>
    /// 竖排书写；Vertical writing.
    /// </summary>
    VerticalWriting = 0x76657274, // 'vert'

    /// <summary>
    /// 替代竖排半宽度量；Alternate vertical half metrics.
    /// </summary>
    AlternateVerticalHalfMetrics = 0x7668616c, // 'vhal'

    /// <summary>
    /// 元音 Jamo 形式；Vowel Jamo forms.
    /// </summary>
    VowelJamoForms = 0x766a6d6f, // 'vjmo'

    /// <summary>
    /// 竖排假名替代；Vertical kana alternates.
    /// </summary>
    VerticalKanaAlternates = 0x766b6e61, // 'vkna'

    /// <summary>
    /// 竖排字距调整；Vertical kerning.
    /// </summary>
    VerticalKerning = 0x766b726e, // 'vkrn'

    /// <summary>
    /// 比例替代竖排度量；Proportional alternate vertical metrics.
    /// </summary>
    ProportionalAlternateVerticalMetrics = 0x7670616c, // 'vpal'

    /// <summary>
    /// 竖排旋转；Vertical rotation.
    /// </summary>
    VerticalRotation = 0x76727432, // 'vrt2'

    /// <summary>
    /// 斜杠零；Slashed zero.
    /// </summary>
    SlashedZero = 0x7a65726f, // 'zero'
}