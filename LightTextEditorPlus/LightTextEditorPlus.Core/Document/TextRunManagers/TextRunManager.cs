using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Document;

internal class TextRunManager
{
    // DocumentRunEditProvider
    public TextRunManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public ParagraphManager ParagraphManager => TextEditor.DocumentManager.ParagraphManager;
    public TextEditorCore TextEditor { get; }

    public void Append(IImmutableRun run)
    {
        // 追加是使用最多的，需要做额外的优化
        var lastParagraph = ParagraphManager.GetLastParagraph();

        // 追加的样式继承规则：
        // 1. 如果当前段落是空，那么追加时，继承当前段落的字符属性样式
        // 2. 如果当前段落已有文本，那么追加时，使用此段落最后一个字符的字符属性作为字符属性样式
        IReadOnlyRunProperty styleRunProperty;
        var index = lastParagraph.CharCount - 1;
        if (index < 0)
        {
            // 如果当前段落是空，那么追加时，继承当前段落的字符属性样式
            styleRunProperty = lastParagraph.ParagraphProperty.ParagraphStartRunProperty ??
                               TextEditor.DocumentManager.CurrentRunProperty;
        }
        else
        {
            // 如果当前段落已有文本，那么追加时，使用此段落最后一个字符的字符属性作为字符属性样式
            var charData = lastParagraph.GetCharData(new ParagraphCharOffset(index));
            styleRunProperty = charData.RunProperty;
        }

        AppendRunToParagraph(run, lastParagraph, styleRunProperty);
    }

    public void Replace(Selection selection, IImmutableRun run)
    {
        // 替换的时候，需要处理文本的字符属性样式
        IReadOnlyRunProperty? styleRunProperty = null;
        // 规则：
        // 1. 替换文本时，采用靠近文档的光标的后续一个字符的字符属性
        // 2. 仅加入新文本时，采用光标的前一个字符的字符属性

        // 先执行删除，再执行插入
        if (selection.Length != 0)
        {
            var paragraphDataResult = ParagraphManager.GetHitParagraphData(selection.StartOffset);

            /*
             * 替换文本时，采用靠近文档的光标的后续一个字符的字符属性
             * 0 1 2 3 ------ 光标偏移量
               | | | |
                A B C  ------ 字符 
             * 假设光标是 1 的值，那将取 B 字符，因此换算方法就是获取当前光标的偏移转换
             */
            var paragraphCharOffset = new ParagraphCharOffset(paragraphDataResult.HitOffset.Offset);
            var charData = paragraphDataResult.ParagraphData.GetCharData(paragraphCharOffset);
            styleRunProperty = charData.RunProperty;

            RemoveInner(selection);
        }
        else
        {
            // 没有替换的长度，加入即可
        }

        InsertInner(selection.StartOffset, run, styleRunProperty);
    }

    /// <summary>
    /// 在文档指定位移<paramref name="offset"/>处插入一段文本
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="run"></param>
    /// <param name="styleRunProperty">继承的样式，如果非替换，仅加入，那这是空</param>
    private void InsertInner(CaretOffset offset, IImmutableRun run, IReadOnlyRunProperty? styleRunProperty)
    {
        // 插入的逻辑，找到插入变更的行
        var paragraphDataResult = ParagraphManager.GetHitParagraphData(offset);
        var paragraphData = paragraphDataResult.ParagraphData;

        if (styleRunProperty is null)
        {
            // 仅加入 styleRunProperty 是空
            // 仅加入新文本时，采用光标的前一个字符的字符属性
            /*
             * 仅加入新文本时，采用光标的前一个字符的字符属性
             * 0 1 2 3 ------ 光标偏移量
               | | | |
                A B C  ------ 字符 
             * 假设光标是 1 的值，那将取 A 字符，因此换算方法就是获取当前光标的前面一个字符
             */
            if (paragraphDataResult.HitOffset.Offset == 0)
            {
                // 规定，光标是 0 获取段落的字符属性
                styleRunProperty = paragraphData.ParagraphProperty.ParagraphStartRunProperty ??
                                   TextEditor.DocumentManager.CurrentRunProperty;
            }
            else
            {
                var paragraphCharOffset = new ParagraphCharOffset(paragraphDataResult.HitOffset.Offset - 1);
                var charData = paragraphData.GetCharData(paragraphCharOffset);
                styleRunProperty = charData.RunProperty;
            }
        }

        // 看看是不是在段落中间插入的，如果在段落中间插入的，需要将段落中间移除掉
        // 在插入完成之后，重新加入
        var lastParagraphRunList = paragraphData.SplitRemoveByParagraphOffset(paragraphDataResult.HitOffset);

        // 追加文本，获取追加之后的当前段落
        var currentParagraph = AppendRunToParagraph(run, paragraphData, styleRunProperty);

        if (lastParagraphRunList != null)
        {
            // 如果是从一段的中间插入的，需要将这一段在插入点后面的内容继续放入到当前的段落
            currentParagraph.AppendCharData(lastParagraphRunList);
        }
    }

    /// <summary>
    /// 给段落追加文本
    /// </summary>
    /// <param name="run"></param>
    /// <param name="paragraphData"></param>
    /// <param name="styleRunProperty">如果 <paramref name="run"/> 没有字符属性，将继承使用这个属性</param>
    /// <returns>由于文本追加可能带上换行符，会新加段落。返回当前的段落</returns>
    private ParagraphData AppendRunToParagraph(IImmutableRun run, ParagraphData paragraphData,
        IReadOnlyRunProperty styleRunProperty)
    {
        var runProperty = run.RunProperty ?? styleRunProperty;

        // 当前的段落，如果插入的分行的内容，那自然需要自动分段
        var currentParagraph = paragraphData;
        // 看起来不需要中间插入逻辑，只需要插入到最后
        //var insertOffset = offset;

        // 获取 run 的分段逻辑，大部分情况下都是按照 \r\n 作为分段逻辑
        var runParagraphSplitter = TextEditor.PlatformProvider.GetRunParagraphSplitter();
        bool isFirstSubRun = true;
        foreach (var subRun in runParagraphSplitter.Split(run))
        {
            if (subRun is LineBreakRun || !isFirstSubRun)
            {
                // 如果这是一个分段，那直接插入新的段落
                var newParagraph = ParagraphManager.CreateParagraphAndInsertAfter(currentParagraph);

                // todo 如果有明确的分行，那就给定一个段落的字符属性
                //newParagraph.ParagraphProperty.ParagraphStartRunProperty = runProperty;

                currentParagraph = newParagraph;

                //insertOffset = ParagraphManager.GetParagraphStartDocumentOffset(currentParagraph);
            }
            else
            {
                //paragraphData.InsertRun(insertOffset,subRun);
                //insertOffset += subRun.Count;
            }

            currentParagraph.AppendRun(subRun, runProperty);

            isFirstSubRun = false;
        }

        return currentParagraph;
    }

    private void RemoveInner(Selection selection)
    {
        // todo 实现删除逻辑
    }
}

/// <summary>
/// 字符的布局信息，包括字符所在的段落和所在的行，字符所在的相对于文本框的坐标
/// </summary>
class CharLayoutData : IParagraphCache
{
    public CharLayoutData(CharData charData, ParagraphData paragraph)
    {
        CharData = charData;
        Paragraph = paragraph;
        paragraph.InitVersion(this);
    }

    public CharData CharData { get; }

    internal ParagraphData Paragraph { get; }

    public uint CurrentParagraphVersion { get; set; }

    public bool IsInvalidVersion() => Paragraph.IsInvalidVersion(this);

    public void UpdateVersion() => Paragraph.UpdateVersion(this);

    /// <summary>
    /// 左上角的点，相对于文本框
    /// </summary>
    /// 可用来辅助布局上下标
    public Point StartPoint { set; get; }

    public ParagraphCharOffset CharIndex { set; get; }

    // todo 提供获取是第几行，第几个字符功能

    /// <summary>
    /// 当前所在的行
    /// </summary>
    public LineVisualData? CurrentLine { set; get; }
}

/// <summary>
/// 表示一个 人类语言文化 的字符
/// <para>
/// 有一些字符，如表情，是需要使用两个 char 表示。这里当成一个处理
/// </para>
/// </summary>
public class CharData
{
    public CharData(ICharObject charObject, IReadOnlyRunProperty runProperty)
    {
        CharObject = charObject;
        RunProperty = runProperty;
    }

    public ICharObject CharObject { get; }

    public IReadOnlyRunProperty RunProperty { get; }

    internal CharLayoutData? CharLayoutData { set; get; }

    /// <summary>
    /// 获取当前字符的左上角坐标，坐标相对于文本框。此属性必须是在布局完成之后才能获取
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Point GetStartPoint()
    {
        if (CharLayoutData is null || CharLayoutData.IsInvalidVersion())
        {
            throw new InvalidOperationException($"禁止在开始布局之前获取");
        }

        return CharLayoutData.StartPoint;
    }

    /// <summary>
    /// 设置当前字符的左上角坐标，坐标相对于文本框
    /// </summary>
    /// <param name="point"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetStartPoint(Point point)
    {
        if (CharLayoutData is null)
        {
            throw new InvalidOperationException($"禁止在开始布局之前设置");
        }

        CharLayoutData.StartPoint = point;

        IsSetStartPointInDebugMode = true;
    }

    /// <summary>
    /// 是否已经设置了此字符的起始（左上角）坐标。这是一个调试属性，仅调试下有用
    /// </summary>
    public bool IsSetStartPointInDebugMode { set; get; }

    /// <summary>
    /// 尺寸
    /// </summary>
    /// 尺寸是可以复用的
    public Size? Size
    {
        set
        {
            if (_size != null)
            {
                throw new InvalidOperationException($"禁止重复给尺寸赋值");
            }

            _size = value;
        }
        get => _size;
    }

    private Size? _size;

    /// <summary>
    /// 调试下的判断逻辑
    /// </summary>
    /// <exception cref="TextEditorDebugException"></exception>
    internal void DebugVerify()
    {
        if (CharLayoutData != null)
        {
            if (!ReferenceEquals(CharLayoutData.CharData, this))
            {
                throw new TextEditorDebugException($"此 CharData 存放的渲染数据对应的字符，不是当前的 CharData 数据");
            }
        }
    }
}

/// <summary>
/// 段落里的字符管理
/// </summary>
class ParagraphCharDataManager
{
    public ParagraphCharDataManager(ParagraphData paragraph)
    {
        _paragraph = paragraph;
    }

    // 这个字段没有什么用，更多是给调试使用，防止一个 ParagraphCharDataManager 被在多个段落使用
    private readonly ParagraphData _paragraph;

    // 不公开，后续也许会更换数据类型
    private List<CharData> CharDataList { get; } = new List<CharData>();

    public int CharCount => CharDataList.Count;

    public void Add(CharData charData)
    {
        Debug.Assert(charData.CharLayoutData is null, "一个 CharData 不会被加入两次");
        charData.CharLayoutData = new CharLayoutData(charData, _paragraph);
        CharDataList.Add(charData);
    }

    public void AddRange(IEnumerable<CharData> charDataList)
    {
        foreach (var charData in charDataList)
        {
            Add(charData);
        }

        //CharDataList.AddRange(charDataList);
    }

    public void RemoveRange(int index, int count) => CharDataList.RemoveRange(index, count);

    public IList<CharData> GetRange(int index, int count) => CharDataList.GetRange(index, count);

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start) =>
        ToReadOnlyListSpan(start, CharDataList.Count - start);

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start, int length) =>
        new ReadOnlyListSpan<CharData>(CharDataList, start, length);

    public CharData GetCharData(int offset)
    {
        return CharDataList[offset];
    }
}

/// <summary>
/// 段落的渲染数据
/// </summary>
class ParagraphLayoutData
{
    public Point StartPoint { set; get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    public Size Size { set; get; }

    public Rect GetBounds() => new Rect(StartPoint, Size);
}

/// <summary>
/// 行渲染信息
/// </summary>
class LineVisualData : IParagraphCache
{
    /// <summary>
    /// 行渲染信息
    /// </summary>
    /// <param name="currentParagraph">行所在的段落</param>
    public LineVisualData(ParagraphData currentParagraph)
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
        return $"{nameof(LineVisualData)}: {GetText()}";
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

    public LineDrawnArgument GetLineDrawnArgument()
    {
        return new LineDrawnArgument(IsDrawn, IsLineStartPointUpdated, LineAssociatedRenderData, StartPoint, Size,
            GetCharList());
    }
}