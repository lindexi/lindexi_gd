using System;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

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
    /// 对外应该使用 ParagraphCharOffset 类型哦
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
    /// 左侧贴边，上侧为文档坐标。依靠各个 Thickness 属性来计算具体的位置
    /// 即表示整个行的起始点。而具体的字符渲染的起始点，需要加上 <see cref="IndentationThickness"/> 和 <see cref="HorizontalTextAlignmentGapThickness"/> 的值，即使用 <see cref="LineContentStartPoint"/> 属性
    /// 关系图请参阅 《文本库行布局信息定义.enbx》 文档
    public TextPointInHorizontalArrangingCoordinateSystem CharStartPoint
    {
        get => CharStartPointInParagraphCoordinateSystem.ToDocumentPoint(CurrentParagraph);
    }

    /// <summary>
    /// 相对于段落的文本范围的起始点
    /// </summary>
    internal TextPointInParagraphCoordinateSystem CharStartPointInParagraphCoordinateSystem
    {
        set
        {
            if (_charStartPoint == value)
            {
                return;
            }

            _charStartPoint = value;
            IsLineStartPointUpdated = true;
        }
        get => _charStartPoint;
    }

    private TextPointInParagraphCoordinateSystem _charStartPoint;

    /// <summary>
    /// 这一行的尺寸。这是在 <see cref="LineCharTextSize"/> 基础上叠加行距尺寸信息。但有时候某些字体的 <see cref="LineCharTextSize"/> 会大于行尺寸，其根本原因是 <see cref="LineCharTextSize"/> 是字符的尺寸，直接就取字符尺寸。而 <see cref="LineContentSize"/> 是取行距计算 <see cref="ILineSpacingCalculator"/> 获取的，不一定会大于字符尺寸。所谓叠加行距尺寸信息，不是直接在字符尺寸加上行距尺寸，而是通过 <see cref="ILineSpacingCalculator"/> 计算得到的
    /// </summary>
    /// 包含以下缩进：
    /// - IndentationThickness - 表示缩进带来的空白
    /// - HorizontalTextAlignmentGapThickness - 表示对齐带来的空白，如居中对齐
    /// - LineSpacingThickness - 表示行距带来的空白
    ///
    /// 内容尺寸什么时候可以大于 <see cref="OutlineBounds"/> 宽度？当 <see cref="ParagraphProperty.AllowHangingPunctuation"/> 允许符号溢出边界时
    public TextSize LineContentSize { get; init; }

    /// <summary>
    /// 这一行的字符尺寸，不会受到行距的影响
    /// </summary>
    public TextSize LineCharTextSize { get; init; }

    /// <summary>
    /// 行内有内容的起始点。等于 <see cref="CharStartPoint"/> 加上 <see cref="IndentationThickness"/> 和 <see cref="HorizontalTextAlignmentGapThickness"/> 的值
    /// </summary>
    /// 关系图请参阅 《文本库行布局信息定义.enbx》 文档
    public TextPointInHorizontalArrangingCoordinateSystem LineContentStartPoint
    {
        get
        {
            var charStartPoint = CharStartPoint;

            var offsetX = IndentationThickness.Left + HorizontalTextAlignmentGapThickness.Left;
            var offsetY = IndentationThickness.Top + HorizontalTextAlignmentGapThickness.Top;
            return charStartPoint.Offset(offsetX,offsetY);
        }
    }

    /// <summary>
    /// 这一行的有内容的范围。这个范围包裹住了文本的内容，包括行距尺寸信息
    /// </summary>
    public TextRect GetLineContentBounds()
    {
        TextPoint currentArrangingTypePoint = LineContentStartPoint.ToCurrentArrangingTypePoint();
        return new TextRect(currentArrangingTypePoint, LineContentSize);
    }

    /// <summary>
    /// 这一行的包括对齐产生的空白 Gap 的范围，为最大的范围。包括以下空白部分：
    /// - 段落的左右边距 <see cref="ParagraphProperty.LeftIndentation"/> 和 <see cref="ParagraphProperty.RightIndentation"/>
    /// - 段落左中右对齐带来的空白 <see cref="ParagraphProperty.HorizontalTextAlignment"/>
    /// - 段落缩进带来的空白 <see cref="ParagraphProperty.Indent"/>
    /// </summary>
    public TextRect OutlineBounds =>
        new TextRect(OutlineStartPointInParagraphCoordinateSystem.ToDocumentPoint(CurrentParagraph).ToCurrentArrangingTypePoint(), OutlineSize);

    public void SetOutlineBounds(TextPointInParagraphCoordinateSystem outlineStartPointInParagraphCoordinateSystem, TextSize outlineSize)
    {
        OutlineStartPointInParagraphCoordinateSystem = outlineStartPointInParagraphCoordinateSystem;
        OutlineSize = outlineSize;
    }
    public TextPointInParagraphCoordinateSystem OutlineStartPointInParagraphCoordinateSystem { get; private set; }
    public TextSize OutlineSize { get; private set; }

    /// <summary>
    /// 这一行的缩进信息带来的边距
    /// </summary>
    public TextThickness IndentationThickness { get; private set; }

    /// <summary>
    /// 这一行的对齐信息带来的边距
    /// </summary>
    public TextThickness HorizontalTextAlignmentGapThickness { get; private set; }

    /// <summary>
    /// 表示行距带来的空白
    /// </summary>
    public TextThickness LineSpacingThickness { get; init; }

    /// <summary>
    /// 设置行最终布局信息
    /// </summary>
    /// <param name="indentationThickness"></param>
    /// <param name="horizontalTextAlignmentGapThickness"></param>
    public void SetLineFinalLayoutInfo(TextThickness indentationThickness, TextThickness horizontalTextAlignmentGapThickness)
    {
        IndentationThickness = indentationThickness;
        HorizontalTextAlignmentGapThickness = horizontalTextAlignmentGapThickness;
    }

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
        return new LineDrawingArgument(IsDrawn, IsLineStartPointUpdated, LineAssociatedRenderData, CharStartPoint.ToCurrentArrangingTypePoint(), 
            GetCharList())
        {
            LineContentSize = LineContentSize,
            LineCharTextSize = LineCharTextSize,
        };
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
