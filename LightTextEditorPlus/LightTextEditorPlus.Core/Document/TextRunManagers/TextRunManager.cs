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

    public void Append(IImmutableRun run)
    {
        // 追加是使用最多的，需要做额外的优化
        var lastParagraph = ParagraphManager.GetLastParagraph();
        var index = lastParagraph.CharCount -1;
        IReadOnlyRunProperty styleRunProperty;
        if (index < 0)
        {
            styleRunProperty = lastParagraph.ParagraphProperty.ParagraphStartRunProperty ??
                          TextEditor.DocumentManager.CurrentRunProperty;
        }
        else
        {
            var charData = lastParagraph.GetCharData(new ParagraphOffset(index));
            styleRunProperty = charData.RunProperty;
        }
        AppendRunToParagraph(run, lastParagraph,styleRunProperty);
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
            // todo 这里的 HitOffset 是存在加一问题的
            var charData = paragraphDataResult.ParagraphData.GetCharData(paragraphDataResult.HitOffset);
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
            // todo 这里可能存在加一问题，导致获取到后面的字符
            var charData = paragraphData.GetCharData(paragraphDataResult.HitOffset);
            styleRunProperty = charData.RunProperty;
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
    private ParagraphData AppendRunToParagraph(IImmutableRun run, ParagraphData paragraphData, IReadOnlyRunProperty styleRunProperty)
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
            var paragraphData = CreateParagraphAndInsertAfter(relativeParagraph: null);
            ParagraphList.Add(paragraphData);
            return GetResult(paragraphData);
        }
        else
        {
            if (offset.Offset == 0)
            {
                return GetResult(ParagraphList[0]);
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
                        // todo 这里是存在加一问题的，不应该让光标和字符坐标直接转换
                        var hitParagraphOffset = offset.Offset - currentDocumentOffset;

                        return GetResult(paragraphData, new ParagraphOffset(hitParagraphOffset));
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

        HitParagraphDataResult GetResult(ParagraphData paragraphData, ParagraphOffset? hitOffset = null)
        {
            return new HitParagraphDataResult(offset, paragraphData, hitOffset ?? new ParagraphOffset(0), this);
        }
    }

    /// <summary>
    /// 获取最后一段。如果这个文本连一段都没有，那就创建新的一段
    /// </summary>
    /// <returns></returns>
    public ParagraphData GetLastParagraph()
    {
        if (ParagraphList.Count == 0)
        {
            var paragraphData = CreateParagraphAndInsertAfter(relativeParagraph: null);
            return paragraphData;
        }
        else
        {
            // ReSharper disable once UseIndexFromEndExpression
            return ParagraphList[ParagraphList.Count - 1];
        }
    }

    /// <summary>
    /// 创建段落且插入到某个段落后面
    /// </summary>
    /// <param name="relativeParagraph">相对的段落，如果是空，那将插入到最后</param>
    /// <returns></returns>
    public ParagraphData CreateParagraphAndInsertAfter(ParagraphData? relativeParagraph)
    {
        ParagraphProperty? paragraphProperty = relativeParagraph?.ParagraphProperty;
        if (paragraphProperty == null)
        {
            // 不优化语法，方便加上断点

            // 获取当前的段落属性作为默认段落属性
            paragraphProperty = TextEditor.DocumentManager.CurrentParagraphProperty;
        }

        var paragraphData = new ParagraphData(paragraphProperty, this);

        if (relativeParagraph is null)
        {
            ParagraphList.Add(paragraphData);
        }
        else
        {
            var index = relativeParagraph.Index;
            ParagraphList.Insert(index + 1, paragraphData);
        }

        return paragraphData;
    }

    public IReadOnlyList<ParagraphData> GetParagraphList() => ParagraphList;

    private List<ParagraphData> ParagraphList { get; } = new List<ParagraphData>();

    //public void InsertParagraphAfter(ParagraphData currentParagraph, ParagraphData newParagraph)
    //{
    //    var index = ParagraphList.IndexOf(currentParagraph);
    //    ParagraphList.Insert(index + 1, newParagraph);
    //}

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

    public int GetParagraphIndex(ParagraphData paragraphData)
    {
        var index = ParagraphList.IndexOf(paragraphData);
        return index;
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

    public ParagraphOffset CharIndex { set; get; }

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
/// 段落数据
/// </summary>
[DebuggerDisplay("第{Index}段，文本：{GetText()}")]
class ParagraphData
{
    public ParagraphData(ParagraphProperty paragraphProperty, ParagraphManager paragraphManager)
    {
        ParagraphProperty = paragraphProperty;
        ParagraphManager = paragraphManager;

        CharDataManager = new ParagraphCharDataManager(this);
    }

    public ParagraphLayoutData ParagraphLayoutData { get; } = new ParagraphLayoutData();

    // todo 还没开始写段落样式
    public ParagraphProperty ParagraphProperty { set; get; }
    public ParagraphManager ParagraphManager { get; }

    public int Index => ParagraphManager.GetParagraphIndex(this);

    private TextEditorCore TextEditor => ParagraphManager.TextEditor;

    /// <summary>
    /// 段落的字符管理
    /// </summary>
    private ParagraphCharDataManager CharDataManager { get; }

    //public IReadOnlyList<IImmutableRun> GetRunList() => TextRunList;

    public CharData GetCharData(ParagraphOffset offset)
    {
        return CharDataManager.GetCharData(offset.Offset);
    }

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start) =>
        CharDataManager.ToReadOnlyListSpan(start);

    public ReadOnlyListSpan<CharData> ToReadOnlyListSpan(int start, int length) =>
        CharDataManager.ToReadOnlyListSpan(start, length);

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
            var count = CharCount - offset.Offset;
            var charDataList = CharDataManager.GetRange(offset.Offset, count);
            CharDataManager.RemoveRange(offset.Offset, count);

            foreach (var charData in charDataList)
            {
                // 这里也许可以考虑一下设置脏的
                var lineVisualData = charData.CharLayoutData?.CurrentLine;
                //if (lineVisualData != null)
                //{
                //    lineVisualData.IsDirty = true;
                //}

                charData.CharLayoutData = null;
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

    ///// <summary>
    ///// 在指定的地方插入一段文本
    ///// </summary>
    ///// <param name="insertOffset"></param>
    ///// <param name="run"></param>
    //public void InsertRun(int insertOffset, IImmutableRun run)
    //{
    //    if (insertOffset == CharCount)
    //    {
    //        AppendRun(run);
    //    }
    //    else
    //    {
    //        throw new NotImplementedException($"还没实现从段落中间插入的功能");
    //    }

    //    // todo 设置LineVisualData是脏的

    //    SetDirty();
    //}

    public void AppendRun(IImmutableRun run, IReadOnlyRunProperty runProperty)
    {
        //var runProperty = run.RunProperty ??
        //                  ParagraphProperty.ParagraphStartRunProperty ?? TextEditor.DocumentManager.CurrentRunProperty;

        //TextRunList.Add(run);
        for (int i = 0; i < run.Count; i++)
        {
            var charObject = run.GetChar(i).DeepClone();
            var charData = new CharData(charObject, runProperty);
            AppendCharData(charData);
        }

        SetDirty();
    }

    //public void AppendRun(IList<IImmutableRun> runList)
    //{
    //    foreach (var run in runList)
    //    {
    //        AppendRun(run);
    //    }

    //    SetDirty();
    //}

    internal void AppendCharData(CharData charData)
    {
        // 谨慎 CharData 的加入开放给框架之外，原因在于 CharData 不能被加入到文本多次
        // 一个 CharData 只能存在一个文本，且只存在一次。原因是 CharData 里面包含了渲染布局信息
        // 加入多次将会让布局很乱
        if (charData.CharLayoutData is not null)
        {
            throw new ArgumentException($"此 CharData 已经被加入到某个段落，不能重复加入");
        }

        CharDataManager.Add(charData);
    }

    internal void AppendCharData(IEnumerable<CharData> charDataList)
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

    /// <summary>
    /// 是否是脏的，需要重新布局渲染
    /// </summary>
    public bool IsDirty()
        // 如果已经布局的版本不等于当前版本，那就是此段文本是脏的
        => IsInvalidVersion(_updatedLayoutVersion);

    /// <summary>
    /// 设置当前文本段是脏的
    /// </summary>
    public void SetDirty()
    {
        if (!IsDirty())
        {
            Version++;
        }
    }

    /// <summary>
    /// 设置当前布局完成，布局完成就不是脏的
    /// </summary>
    public void SetFinishLayout()
    {
        _updatedLayoutVersion = Version;
    }

    /// <summary>
    /// 已经更新布局的版本
    /// </summary>
    private uint _updatedLayoutVersion = 0;


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

public readonly record struct LineDrawnArgument(bool IsDrawn, bool IsLineStartPointUpdated,
    object? LineAssociatedRenderData, Point StartPoint, Size Size, ReadOnlyListSpan<CharData> CharList)
{
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