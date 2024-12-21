using System;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;

// ReSharper disable All

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 行渲染信息
/// </summary>
class LineLayoutData : IParagraphCache, IDisposable
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

    public void SetDirty() => CurrentParagraphVersion = 0;
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
    public TextPoint CharStartPoint
    {
        set
        {
            _charStartPoint = value;
            IsLineStartPointUpdated = true;
        }
        get => _charStartPoint;
    }

    private TextPoint _charStartPoint;

    /// <summary>
    /// 这一行的字符尺寸
    /// </summary>
    public TextSize LineCharTextSize { get; init; }

    /// <summary>
    /// 这一行的范围
    /// </summary>
    public TextRect GetLineBounds() => new TextRect(_charStartPoint, LineCharTextSize);

    /// <summary>
    /// 这一行是当前段落的第几行
    /// </summary>
    public int LineInParagraphIndex
    {
        get
        {
            if (IsDirty)
            {
                // 理论上框架内不会进入此分支，于是可以在 get 方法抛出异常
                // 业务层无法访问到这个属性
                throw new TextEditorDirtyException(CurrentParagraph.ParagraphManager.TextEditor);
            }

            return CurrentParagraph.LineLayoutDataList.FindIndex(t => ReferenceEquals(t, this));
        }
    }

    /// <summary>
    /// 获取这一行的字符列表
    /// </summary>
    /// <remarks>这个方法调用接近不用钱，随便调用</remarks>
    /// <returns></returns>
    public TextReadOnlyListSpan<CharData> GetCharList() =>
        CurrentParagraph.ToReadOnlyListSpan(new ParagraphCharOffset(CharStartParagraphIndex), CharEndParagraphIndex - CharStartParagraphIndex);

    public ParagraphCaretOffset ToParagraphCaretOffset(LineCaretOffset lineCaretOffset)
    {
        // 需要自动设置为不超过行的坐标
        var offset = Math.Min(CharCount, lineCaretOffset.Offset);

        return new ParagraphCaretOffset(CharStartParagraphIndex + offset);
    }

    public CaretOffset ToCaretOffset(LineCaretOffset lineCaretOffset)
    {
        var paragraphCaretOffset = ToParagraphCaretOffset(lineCaretOffset);
        return CurrentParagraph.ToCaretOffset(paragraphCaretOffset);
    }

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
        return new LineDrawingArgument(IsDrawn, IsLineStartPointUpdated, LineAssociatedRenderData, CharStartPoint, LineCharTextSize,
            GetCharList());
    }

    #endregion

#if DEBUG
    /// <summary>
    /// 调试下，用来了解有那些字符
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private TextReadOnlyListSpan<CharData> DebugCharList => GetCharList();
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

    public void Dispose()
    {
        // 如果关联的是需要释放的资源，那就调用释放
        if (LineAssociatedRenderData is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}