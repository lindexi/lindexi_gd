using System.Text;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
// ReSharper disable All

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
    public uint CurrentParagraphVersion { get; set; }

    /// <summary>
    /// 此行包含的字符，字符在段落中的起始点
    /// </summary>
    /// 为什么使用段落做相对？原因是如果使用文档的字符起始点或结束点，在当前段落之前新插入一段，那将会导致信息需要重新更新。如果使用的是段落的起始点，那在当前段落之前插入，就不需要更新行的信息
    public int CharStartParagraphIndex { init; get; } = -1;

    /// <summary>
    /// 此行包含的字符，字符在段落中的结束点
    /// </summary>
    public int CharEndParagraphIndex { init; get; } = -1;

    /// <summary>
    /// 这一行的字符长度
    /// </summary>
    public int CharCount => CharEndParagraphIndex - CharStartParagraphIndex;

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
    /// 这一行的尺寸
    /// </summary>
    public Size Size { get; init; }

    public ReadOnlyListSpan<CharData> GetCharList() =>
        CurrentParagraph.ToReadOnlyListSpan(CharStartParagraphIndex, CharEndParagraphIndex - CharStartParagraphIndex);

    #region 绘制渲染

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
    public bool IsLineStartPointUpdated { get; private set; }
    // 一行的起始点，明确初始值是没有变更的。强行赋值，虽然没有实际的代码逻辑意义，但用来说明这个属性默认就是没有变更
        = false;

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
    public LineDrawingArgument GetLineDrawingArgument()
    {
        return new LineDrawingArgument(IsDrawn, IsLineStartPointUpdated, LineAssociatedRenderData, StartPoint, Size,
            GetCharList());
    }

    #endregion

    //public List<RunVisualData>? RunVisualDataList { set; get; }

    //public Span<IImmutableRun> GetSpan()
    //{
    //    //return CurrentParagraph.AsSpan().Slice(StartParagraphIndex, EndParagraphIndex - StartParagraphIndex);
    //    return CurrentParagraph.AsSpan()[StartParagraphIndex..EndParagraphIndex];
    //}

#if DEBUG
    /// <summary>
    /// 调试下，用来了解有那些字符
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private ReadOnlyListSpan<CharData> DebugCharList => GetCharList();
#endif

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
}