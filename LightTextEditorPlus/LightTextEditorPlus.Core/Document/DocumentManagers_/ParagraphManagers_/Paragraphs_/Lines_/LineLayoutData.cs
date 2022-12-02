using System.Text;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 行渲染信息
/// </summary>
class LineLayoutData : IParagraphCache
{
    /// <summary>
    /// 行渲染信息
    /// </summary>
    /// <param name="currentParagraph">行所在的段落</param>
    public LineLayoutData(ParagraphData currentParagraph)
    {
        CurrentParagraph = currentParagraph;
        currentParagraph.InitVersion(this);
    }

    public ParagraphData CurrentParagraph { get; }

    /// <summary>
    /// 是否是脏的，需要重新布局渲染
    /// </summary>
    public bool IsDirty => CurrentParagraph.IsInvalidVersion(this);

    public void UpdateVersion() => CurrentParagraph.UpdateVersion(this);

    public int StartParagraphIndex { init; get; } = -1;

    public int EndParagraphIndex { init; get; } = -1;

    /// <summary>
    /// 这一行的字符长度
    /// </summary>
    public int CharCount => EndParagraphIndex - StartParagraphIndex;

    /// <summary>
    /// 这一行的起始的点，相对于文本框
    /// </summary>
    public Point StartPoint
    {
        set
        {
            _startPoint = value;
            IsLineStartPointUpdated = true;
        }
        get => _startPoint;
    }

    private Point _startPoint;

    /// <summary>
    /// 是否需要绘制
    /// </summary>
    /// 如果没有画过，或者是行的起始点变更，需要绘制
    public bool NeedDraw => !IsDrawn || IsLineStartPointUpdated;

    /// <summary>
    /// 是否已经画过
    /// </summary>
    public bool IsDrawn { get; private set; }

    /// <summary>
    /// 是否行的起始点变更
    /// </summary>
    public bool IsLineStartPointUpdated { get; private set; } = false;

    /// <summary>
    /// 行的关联渲染信息
    /// </summary>
    public object? LineAssociatedRenderData { private set; get; }

    /// <summary>
    /// 设置已经画完了
    /// </summary>
    public void SetDrawn(in LineDrawnResult result)
    {
        IsDrawn = true;
        IsLineStartPointUpdated = false;

        LineAssociatedRenderData = result.LineAssociatedRenderData;
    }

    /// <summary>
    /// 这一行的尺寸
    /// </summary>
    public Size Size { get; init; }

    //public List<RunVisualData>? RunVisualDataList { set; get; }

    //public Span<IImmutableRun> GetSpan()
    //{
    //    //return CurrentParagraph.AsSpan().Slice(StartParagraphIndex, EndParagraphIndex - StartParagraphIndex);
    //    return CurrentParagraph.AsSpan()[StartParagraphIndex..EndParagraphIndex];
    //}

    public ReadOnlyListSpan<CharData> GetCharList() =>
        CurrentParagraph.ToReadOnlyListSpan(StartParagraphIndex, EndParagraphIndex - StartParagraphIndex);

    public override string ToString()
    {
        return $"{nameof(LineLayoutData)}: {GetText()}";
    }

    public string GetText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var charData in GetCharList())
        {
            stringBuilder.Append(charData.CharObject.ToText());
        }

        return stringBuilder.ToString();
    }

    public uint CurrentParagraphVersion { get; set; }

    public LineDrawingArgument GetLineDrawingArgument()
    {
        return new LineDrawingArgument(IsDrawn, IsLineStartPointUpdated, LineAssociatedRenderData, StartPoint, Size,
            GetCharList());
    }
}