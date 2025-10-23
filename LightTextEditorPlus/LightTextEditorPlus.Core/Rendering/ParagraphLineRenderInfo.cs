using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 段落的行渲染信息
/// </summary>
public readonly struct ParagraphLineRenderInfo
{
    /// <summary>
    /// 段落的行渲染信息
    /// </summary>
    /// <param name="lineIndex">这一行是段落的第几行，从0开始</param>
    /// <param name="paragraphIndex">当前是在第几个段落</param>
    /// <param name="argument">行渲染参数</param>
    /// <param name="paragraphStartRunProperty"></param>
    /// <param name="lineLayoutData"></param>
    /// <param name="renderInfoProvider"></param>
    internal ParagraphLineRenderInfo(int lineIndex, ParagraphIndex paragraphIndex, LineDrawingArgument argument,
        LineLayoutData lineLayoutData, IReadOnlyRunProperty paragraphStartRunProperty,
        RenderInfoProvider renderInfoProvider)
    {
        LineIndex = lineIndex;
        ParagraphIndex = paragraphIndex;
        Argument = argument;
        LineLayoutData = lineLayoutData;
        ParagraphStartRunProperty = paragraphStartRunProperty;
        _renderInfoProvider = renderInfoProvider;
    }

    /// <summary>
    /// 内部使用的行信息
    /// </summary>
    /// 由于需要修改访问权限，修改为属性
    internal LineLayoutData LineLayoutData { get; }

    /// <summary>
    /// 当前行所在的段落属性
    /// </summary>
    public ParagraphProperty ParagraphProperty => Paragraph.ParagraphProperty;

    internal ParagraphData Paragraph => LineLayoutData.CurrentParagraph;

    /// <summary>
    /// 当前行所在的段落
    /// </summary>
    public ITextParagraph CurrentParagraph => Paragraph;

    /// <summary>
    /// 获取当前行所在的段落的渲染信息
    /// </summary>
    /// <returns></returns>
    public ParagraphRenderInfo GetCurrentParagraphRenderInfo()
        => new ParagraphRenderInfo(ParagraphIndex, Paragraph, _renderInfoProvider);

    /// <summary>这一行是段落的第几行，从0开始</summary>
    public int LineIndex { get; }

    /// <summary>
    /// 是否首行
    /// </summary>
    public bool IsFirstLine => LineIndex == 0;

    /// <summary>
    /// 当前是在第几个段落，从0开始
    /// </summary>
    public ParagraphIndex ParagraphIndex { get; }

    /// <summary>行渲染参数</summary>
    public LineDrawingArgument Argument { get; }

    /// <inheritdoc cref="LineLayoutData.GetLineContentBounds"/>
    public TextRect ContentBounds => LineLayoutData.GetLineContentBounds();

    /// <inheritdoc cref="LineLayoutData.OutlineBounds"/>
    public TextRect OutlineBounds => LineLayoutData.OutlineBounds;

    /// <inheritdoc cref="LineDrawingArgument.CharList"/>
    /// <remarks>
    /// 完全等价于 <see cref="Argument"/> 的 <see cref="LineDrawingArgument.CharList"/> 属性
    /// </remarks>
    public TextReadOnlyListSpan<CharData> CharList => Argument.CharList;

    /// <summary>
    /// 段落起始字符信息
    /// </summary>
    public IReadOnlyRunProperty ParagraphStartRunProperty { get; }

    /// <summary>
    /// 项目符号运行时信息
    /// </summary>
    internal MarkerRuntimeInfo? MarkerRuntimeInfo => Paragraph.MarkerRuntimeInfo;

    /// <summary>
    /// 是否包含项目符号。段内首行并且有项目符号
    /// </summary>
    public bool IsIncludeMarker => IsFirstLine && MarkerRuntimeInfo != null;

    private readonly RenderInfoProvider _renderInfoProvider;

    /// <summary>
    /// 获取项目符号字符内容
    /// </summary>
    /// <returns></returns>
    public TextReadOnlyListSpan<CharData> GetMarkerCharDataList()
    {
        return MarkerRuntimeInfo?.CharDataList ?? new TextReadOnlyListSpan<CharData>([], 0, 0);
    }

    /// <summary>
    /// 设置渲染结果
    /// </summary>
    /// <param name="lineDrawnResult"></param>
    public void SetDrawnResult(in LineDrawnResult lineDrawnResult)
    {
        LineLayoutData.SetDrawn(lineDrawnResult);
    }
}
