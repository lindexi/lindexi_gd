using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;

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
    internal LineLayoutData LineLayoutData {  get; }

    /// <summary>
    /// 当前行所在的段落属性
    /// </summary>
    public ParagraphProperty ParagraphProperty => LineLayoutData.CurrentParagraph.ParagraphProperty;

    /// <summary>
    /// 获取当前行所在的段落的渲染信息
    /// </summary>
    /// <returns></returns>
    public ParagraphRenderInfo GetCurrentParagraphRenderInfo()
        => new ParagraphRenderInfo(ParagraphIndex, LineLayoutData.CurrentParagraph, _renderInfoProvider);

    /// <summary>这一行是段落的第几行，从0开始</summary>
    public int LineIndex { get;}

    /// <summary>
    /// 当前是在第几个段落，从0开始
    /// </summary>
    public ParagraphIndex ParagraphIndex { get; }

    /// <summary>行渲染参数</summary>
    public LineDrawingArgument Argument { get; }

    /// <summary>
    /// 段落起始字符信息
    /// </summary>
    public IReadOnlyRunProperty ParagraphStartRunProperty { get; }

    private readonly RenderInfoProvider _renderInfoProvider;

    /// <summary>
    /// 设置渲染结果
    /// </summary>
    /// <param name="lineDrawnResult"></param>
    public void SetDrawnResult(in LineDrawnResult lineDrawnResult)
    {
        LineLayoutData.SetDrawn(lineDrawnResult);
    }
}
