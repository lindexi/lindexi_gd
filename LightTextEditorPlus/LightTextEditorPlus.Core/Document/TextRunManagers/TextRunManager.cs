using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document;

internal class TextRunManager
{
    public TextRunManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
        ParagraphManager = new ParagraphManager(textEditor);
    }

    public ParagraphManager ParagraphManager { get; }
    public TextEditorCore TextEditor { get; }

    public void Replace(Selection selection, IImmutableRun run)
    {
        // 先执行删除，再执行插入
        if (selection.Length != 0)
        {
            RemoveInner(selection);
        }
        else
        {
            // 没有替换的长度，加入即可
        }

        InsertInner(selection.StartOffset, run);
    }

    /// <summary>
    /// 在文档指定位移<paramref name="offset"/>处插入一段文本
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="run"></param>
    private void InsertInner(CaretOffset offset, IImmutableRun run)
    {
        // 插入的逻辑，找到插入变更的行
        var paragraphDataResult = ParagraphManager.GetHitParagraphData(offset);
        var paragraphData = paragraphDataResult.ParagraphData;

        // 看看是不是在段落中间插入的，如果在段落中间插入的，需要将段落中间移除掉
        // 在插入完成之后，重新加入
        var lastParagraphRunList = paragraphData.SplitRemoveByParagraphOffset(paragraphDataResult.HitOffset);

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
                var newParagraph = ParagraphManager.CreateParagraphData(paragraphData.ParagraphProperty);
                ParagraphManager.InsertParagraphAfter(currentParagraph, newParagraph);

                currentParagraph = newParagraph;

                //insertOffset = ParagraphManager.GetParagraphStartDocumentOffset(currentParagraph);
            }
            else
            {
                //paragraphData.InsertRun(insertOffset,subRun);
                //insertOffset += subRun.Count;
                paragraphData.AppendRun(subRun);
            }

            isFirstSubRun = false;
        }

        if (lastParagraphRunList != null)
        {
            // 如果是从一段的中间插入的，需要将这一段在插入点后面的内容继续放入到当前的段落
            currentParagraph.AppendCharData(lastParagraphRunList);
        }
    }

    private void RemoveInner(Selection selection)
    {
        // todo 实现删除逻辑
    }
}

/// <summary>
/// 段落管理
/// </summary>
[DebuggerDisplay("{GetText()}")]
class ParagraphManager
{
    public ParagraphManager(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    public TextEditorCore TextEditor { get; }

    /// <summary>
    /// 获取被指定的光标偏移命中的段落信息
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public HitParagraphDataResult GetHitParagraphData(CaretOffset offset)
    {
        if (ParagraphList.Count == 0)
        {
            var paragraphData = CreateParagraphData();
            ParagraphList.Add(paragraphData);
            return ResultResult(paragraphData);
        }
        else
        {
            if (offset.Offset == 0)
            {
                return ResultResult(ParagraphList[0]);
            }
            else
            {
                // 判断落在哪个段落里
                // 判断方法就是判断字符范围是否在段落内
                var currentDocumentOffset = 0;
                foreach (var paragraphData in ParagraphList)
                {
                    var endOffset =
                        currentDocumentOffset + paragraphData.CharCount +
                        ParagraphData.DelimiterLength; // todo 这里是否遇到 -1 问题
                    if (offset.Offset < endOffset)
                    {
                        var hitParagraphOffset = offset.Offset - currentDocumentOffset;

                        return ResultResult(paragraphData, new ParagraphOffset(hitParagraphOffset));
                    }
                    else
                    {
                        currentDocumentOffset = endOffset;
                    }
                }
            }

            // 没有落到哪个段落？
            //todo 还没实现落在段落外的逻辑
            throw new NotImplementedException();
        }

        HitParagraphDataResult ResultResult(ParagraphData paragraphData, ParagraphOffset? hitOffset = null)
        {
            return new HitParagraphDataResult(offset, paragraphData, hitOffset ?? new ParagraphOffset(0), this);
        }
    }

    public ParagraphData CreateParagraphData(ParagraphProperty? paragraphProperty = null)
    {
        if (paragraphProperty == null)
        {
            // 不优化语法，方便加上断点

            // 获取当前的段落属性作为默认段落属性
            paragraphProperty = TextEditor.DocumentManager.CurrentParagraphProperty;
        }

        var paragraphData = new ParagraphData(paragraphProperty, this);
        return paragraphData;
    }

    public IReadOnlyList<ParagraphData> GetParagraphList() => ParagraphList;

    private List<ParagraphData> ParagraphList { get; } = new List<ParagraphData>();

    public void InsertParagraphAfter(ParagraphData currentParagraph, ParagraphData newParagraph)
    {
        var index = ParagraphList.IndexOf(currentParagraph);
        ParagraphList.Insert(index + 1, newParagraph);
    }

    /// <summary>
    /// 获取文本行的起始位置在文档中的偏移量，此偏移量的计算考虑了换行符，如123/r/n123，那么第二个段落的Offset为5
    /// </summary>
    /// <exception cref="InvalidOperationException">这个文本行被删除后引发此异常</exception>
    public DocumentOffset GetParagraphStartOffset(ParagraphData paragraphData)
    {
        var offset = 0;
        foreach (var current in ParagraphList)
        {
            if (ReferenceEquals(current, paragraphData))
            {
                return new DocumentOffset(offset);
            }
            else
            {
                offset += current.CharCount + ParagraphData.DelimiterLength;
            }
        }

        // 没有找到段落，证明段落被删除
        throw new InvalidOperationException();
    }

    public string GetText()
    {
        bool isFirst = true;
        var stringBuilder = new StringBuilder();
        foreach (var paragraphData in ParagraphList)
        {
            if (!isFirst)
            {
                stringBuilder.AppendLine();
            }

            paragraphData.GetText(stringBuilder);
            isFirst = false;
        }

        return stringBuilder.ToString();
    }
}

class CharRenderData : IParagraphCache
{
    public CharRenderData(CharData charData, ParagraphData paragraph)
    {
        CharData = charData;
        Paragraph = paragraph;
        paragraph.InitVersion(this);
    }

    public CharData CharData { get; }

    internal ParagraphData Paragraph { get; }

    public uint CurrentParagraphVersion { get; set; }

    public void IsInvalidVersion() => Paragraph.IsInvalidVersion(this);

    public void UpdateVersion() => Paragraph.UpdateVersion(this);

    /// <summary>
    /// 左上角的点，相对于文本框
    /// </summary>
    /// 可用来辅助布局上下标
    public Point LeftTop { set; get; }

    public ParagraphOffset CharIndex { set; get; }

    // todo 提供获取是第几行，第几个字符功能

    /// <summary>
    /// 当前所在的行
    /// </summary>
    public LineVisualData? CurrentLine { set; get; }
}

///// <summary>
///// 字符的可被缓存的渲染信息
///// </summary>
///// 例如尺寸
//class CharRenderCacheData
//{
//    public CharRenderCacheData(Size size)
//    {
//        Size = size;
//    }

//    /// <summary>
//    /// 尺寸
//    /// </summary>
//    /// 尺寸是可以复用的
//    public Size Size { get; }
//}

/// <summary>
/// 表示一个 人类语言文化 的字符
/// <para>
/// 有一些字符，如表情，是需要使用两个 char 表示。这里当成一个处理
/// </para>
/// </summary>
public class CharData
{
    private Size? _size;

    public CharData(ICharObject charObject, IReadOnlyRunProperty runProperty)
    {
        CharObject = charObject;
        RunProperty = runProperty;
    }

    public ICharObject CharObject { get; }

    public IReadOnlyRunProperty RunProperty { get; }

    internal CharRenderData? CharRenderData { set; get; }

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

    internal void DebugVerify()
    {
        if (CharRenderData != null)
        {
            if (!ReferenceEquals(CharRenderData.CharData, this))
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

    public void Add(CharData charData) => CharDataList.Add(charData);
    public void AddRange(IEnumerable<CharData> charDataList)
    {
        CharDataList.AddRange(charDataList);
    }

    public void RemoveRange(int index, int count) => CharDataList.RemoveRange(index, count);

    public IList<CharData> GetRange(int index, int count) => CharDataList.GetRange(index, count);

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start) =>
        ToReadOnlyListSpan(start, CharDataList.Count - start);

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start, int length) =>
        new ReadOnlyListSpan<CharData>(CharDataList, start, length);
}

/// <summary>
/// 段落的渲染数据
/// </summary>
class ParagraphRenderData
{
    public Point LeftTop { set; get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    public Size Size { set; get; }

    public Rect GetBounds() => new Rect(LeftTop, Size);
}

/// <summary>
/// 段落数据
/// </summary>
[DebuggerDisplay("{GetText()}")]
class ParagraphData
{
    public ParagraphData(ParagraphProperty paragraphProperty, ParagraphManager paragraphManager)
    {
        ParagraphProperty = paragraphProperty;
        ParagraphManager = paragraphManager;

        CharDataManager = new ParagraphCharDataManager(this);
    }

    public ParagraphRenderData ParagraphRenderData { get; } = new ParagraphRenderData();

    public ParagraphProperty ParagraphProperty { set; get; }
    public ParagraphManager ParagraphManager { get; }

    private TextEditorCore TextEditor => ParagraphManager.TextEditor;

    /// <summary>
    /// 段落的字符管理
    /// </summary>
    private ParagraphCharDataManager CharDataManager { get; }

    //public IReadOnlyList<IImmutableRun> GetRunList() => TextRunList;

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start) =>
        CharDataManager.ToReadOnlyListSpan(start);

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start, int length) =>
        CharDataManager.ToReadOnlyListSpan(start,length);

    /// <summary>
    /// 这一段的字符长度
    /// </summary>
    public int CharCount => CharDataManager.CharCount;

    /// <summary>
    /// 获取这个文本行是否已经从文档中删除
    /// </summary>
    /// 这个属性更多是给调试
    public bool IsDeleted { set; get; }

    /// <summary>
    /// 是否是脏的，需要重新布局渲染
    /// </summary>
    public bool IsDirty { set; get; } = true;

    /// <summary>
    /// 获取行分隔符的长度
    /// 0 为文档中的最后一行， 1 for <c>"\r"</c> or <c>"\n"</c>, 2 for <c>"\r\n"</c> 
    /// 当本行被删除后这个属性值依然有效，这种情况下，它包含在删除之前的行分隔符的长度
    /// </summary>
    /// 无论在哪个平台上，都统一为 \r\n 两个字符
    public static int DelimiterLength => TextContext.NewLine.Length;

    /// <summary>
    /// 获取本文本行的起始位置在文档中的偏移量，此偏移量的计算考虑了换行符，如123/r/n123，那么第二个段落的Offset为5
    /// </summary>
    /// <exception cref="InvalidOperationException">这个文本行被删除后引发此异常</exception>
    public DocumentOffset GetParagraphStartOffset() => ParagraphManager.GetParagraphStartOffset(this);

    // 不合适，将会让段落必须知道文档坐标
    ///// <summary>
    ///// 从哪个开始
    ///// </summary>
    //public DocumentOffset? DirtyOffset { set; get; }

    // 不需要重复计算从 CaretOffset 转换为段落坐标
    ///// <summary>
    ///// 在段落中间插入的时候，需要将段落在插入后面的内容分割删除
    ///// </summary>
    ///// <param name="offset"></param>
    ///// <exception cref="NotImplementedException"></exception>
    //public IList<IImmutableRun>? SplitRemoveByDocumentOffset(CaretOffset offset)
    //{
    //    // 从光标坐标系，换为段落坐标
    //    DocumentOffset paragraphStartOffset = GetParagraphStartOffset();
    //    // 准确来说，这是段落的光标坐标
    //    var paragraphOffset = offset.Offset - paragraphStartOffset;
    //    var runList = SplitRemoveByDocumentOffset(new ParagraphOffset(paragraphOffset));
    //    return runList;
    //}

    /// <summary>
    /// 在段落中间插入的时候，需要将段落在插入后面的内容分割删除
    /// </summary>
    /// <param name="offset"></param>
    public IList<CharData>? SplitRemoveByParagraphOffset(ParagraphOffset offset)
    {
        // todo 设置LineVisualData是脏的
        if (offset.Offset == CharCount)
        {
            // 如果插入在最后，那就啥都不需要做
            return null;
        }
        else if (offset.Offset > CharCount)
        {
            // 超过段落了
            //todo 处理超过段落
            throw new ArgumentOutOfRangeException(nameof(offset), $"段落字符:{CharCount};Offset={offset.Offset}");
        }
        else
        {
            Version++;
            var count = CharCount-offset.Offset;
            var charDataList = CharDataManager.GetRange(offset.Offset,count);
            CharDataManager.RemoveRange(offset.Offset,count);

            foreach (var charData in charDataList)
            {
                var lineVisualData = charData.CharRenderData?.CurrentLine;
                //if (lineVisualData != null)
                //{
                //    lineVisualData.IsDirty = true;
                //}

                charData.CharRenderData = null;
            }

            return charDataList;
        }
    }

    ///// <summary>
    ///// 在行渲染的时候，将行末的一个 IImmutableRun 按照需求，分割为多个的时候，替换原有的
    ///// </summary>
    //internal void SplitReplace(int paragraphIndex, IImmutableRun firstRun, IImmutableRun secondRun)
    //{
    //    Version++;

    //    TextRunList[paragraphIndex] = firstRun;
    //    TextRunList.Insert(paragraphIndex + 1, secondRun);
    //}

    /// <summary>
    /// 在指定的地方插入一段文本
    /// </summary>
    /// <param name="insertOffset"></param>
    /// <param name="run"></param>
    public void InsertRun(int insertOffset, IImmutableRun run)
    {
        if (insertOffset == CharCount)
        {
            AppendRun(run);
        }
        else
        {
            throw new NotImplementedException($"还没实现从段落中间插入的功能");
        }

        // todo 设置LineVisualData是脏的

        Version++;
    }

    public void AppendRun(IImmutableRun run)
    {
        var runProperty = run.RunProperty ??ParagraphProperty.ParagraphStartRunProperty?? TextEditor.DocumentManager.CurrentRunProperty;

        //TextRunList.Add(run);
        for (int i = 0; i < run.Count; i++)
        {
            var charObject = run.GetChar(i).DeepClone();
            var charData = new CharData(charObject,runProperty);
            AppendCharData(charData);
        }

        Version++;
    }

    public void AppendRun(IList<IImmutableRun> runList)
    {
        foreach (var run in runList)
        {
            AppendRun(run);
        }

        Version++;
    }

    public void AppendCharData(CharData charData)
    {
        CharDataManager.Add(charData);
    }

    public void AppendCharData(IEnumerable<CharData> charDataList)
    {
        CharDataManager.AddRange(charDataList);
    }

    #region 渲染排版数据

    public List<LineVisualData> LineVisualDataList { get; } = new List<LineVisualData>();

    #endregion

    //public ParagraphOffset GetParagraphOffset(IImmutableRun run)
    //{
    //    var paragraphOffset = 0;

    //    foreach (var currentRun in TextRunList)
    //    {
    //        if (ReferenceEquals(currentRun, run))
    //        {
    //            return new ParagraphOffset(paragraphOffset);
    //        }
    //        else
    //        {
    //            paragraphOffset += currentRun.Count;
    //        }
    //    }

    //    throw new ArgumentException($"传入的 Run 不是此段落的元素", nameof(run));
    //}

    ///// <summary>
    ///// 给定传入的段落偏移获取是对应 <see cref="TextRunList"/> 的从哪项开始
    ///// </summary>
    ///// <param name="paragraphOffset"></param>
    ///// <returns></returns>
    //public RunIndexInParagraph GetRunIndex(ParagraphOffset paragraphOffset)
    //{
    //    //var readOnlyListSpan = ToReadOnlyListSpan(0);
    //    //var (run, runIndex, hitIndex) = readOnlyListSpan.GetRunByCharIndex(paragraphOffset.Offset);

    //    //return new RunIndexInParagraph(runIndex, this, run, hitIndex, Version);
    //    return new RunIndexInParagraph(0, this, default, 0, Version);
    //}

    #region Version

    internal bool IsInvalidVersion(uint version) => version != Version;

    internal bool IsInvalidVersion(IParagraphCache cache) => IsInvalidVersion(cache.CurrentParagraphVersion);

    internal void UpdateVersion(IParagraphCache cache)
    {
        if (cache.CurrentParagraphVersion == 0)
        {
            throw new InvalidOperationException($"初始化先调用 {nameof(InitVersion)} 方法");
        }

        cache.CurrentParagraphVersion = Version;
    }

    internal void InitVersion(IParagraphCache cache)
    {
        if (cache.CurrentParagraphVersion != 0)
        {
            throw new InvalidOperationException($"禁止重复初始化");
        }

        cache.CurrentParagraphVersion = Version;
    }

    /// <summary>
    /// 段落的更改版本
    /// </summary>
    private uint Version
    {
        get => _version;
        set
        {
            if (value == 0)
            {
                value = 1;
            }

            _version = value;
        }
    }

    private uint _version = 1;

    #endregion

    public string GetText()
    {
        var stringBuilder = new StringBuilder();

        GetText(stringBuilder);

        return stringBuilder.ToString();
    }

    public void GetText(StringBuilder stringBuilder)
    {
        foreach (var charData in CharDataManager.ToReadOnlyListSpan(0))
        {
            stringBuilder.Append(charData.CharObject.ToText());
        }
    }
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

    public int StartParagraphIndex { set; get; } = -1;

    public int EndParagraphIndex { set; get; } = -1;

    /// <summary>
    /// 这一行的字符长度
    /// </summary>
    public int CharCount => EndParagraphIndex - StartParagraphIndex;

    /// <summary>
    /// 这一行的左上角的点，相对于文本框
    /// </summary>
    public Point LeftTop { set; get; }

    /// <summary>
    /// 这一行的尺寸
    /// </summary>
    public Size Size { get; set; }

    //public List<RunVisualData>? RunVisualDataList { set; get; }

    //public Span<IImmutableRun> GetSpan()
    //{
    //    //return CurrentParagraph.AsSpan().Slice(StartParagraphIndex, EndParagraphIndex - StartParagraphIndex);
    //    return CurrentParagraph.AsSpan()[StartParagraphIndex..EndParagraphIndex];
    //}

    public ReadOnlyListSpan<CharData> GetCharList() => CurrentParagraph.ToReadOnlyListSpan(StartParagraphIndex, EndParagraphIndex);

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
}