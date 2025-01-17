using System;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文档的行距配置
/// </summary>
/// <remarks>
/// 段落行距是 <see cref="ITextLineSpacing"/>
/// </remarks>
public readonly record struct DocumentLineSpacingConfiguration
{
    /// <summary>
    /// 文档的行距配置
    /// </summary>
    public DocumentLineSpacingConfiguration()
    {
    }

    /// <summary>
    /// 当前多倍行距呈现策略
    /// </summary>
    public LineSpacingStrategy LineSpacingStrategy { get; init; } = LineSpacingStrategy.FullExpand;

    /// <summary>
    /// 行距算法
    /// </summary>
    public LineSpacingAlgorithm LineSpacingAlgorithm { get; init; } = LineSpacingAlgorithm.PPT;

    /// <summary>
    /// 字符在行内的垂直对齐方式，此属性的存在仅仅只是为了告诉你，更加正确的是用 <see cref="VerticalCharInLineAlignment"/> 属性而已。根据 [BaselineAlignment Enum (System.Windows) Microsoft Learn](https://learn.microsoft.com/zh-cn/dotnet/api/system.windows.baselinealignment?view=windowsdesktop-9.0 ) 文档可以知道，BaselineAlignment 是作用在 TextRun 上的，而不是整个文本框的全局属性。整个文本框定义的是字符在行内的垂直对齐方式，属于一类样式
    /// </summary>
    [Obsolete("请使用 VerticalCharInLineAlignment 属性", true)]
    public BaselineRatioAlignment BaselineAlignment { get; init; }

    /// <summary>
    /// 字符在行内的垂直对齐方式
    /// </summary>
    public RatioVerticalCharInLineAlignment VerticalCharInLineAlignment
    {
        get
        {
            return _verticalCharInLineAlignment ?? RatioVerticalCharInLineAlignment.BottomAlignment;
        }
        init
        {
            _verticalCharInLineAlignment = value;
        }
    }


    private readonly RatioVerticalCharInLineAlignment? _verticalCharInLineAlignment;
}
