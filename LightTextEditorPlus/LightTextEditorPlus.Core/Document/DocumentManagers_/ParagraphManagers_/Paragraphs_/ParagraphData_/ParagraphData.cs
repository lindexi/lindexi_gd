using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document;

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

    /// <summary>
    /// 段落属性样式
    /// </summary>
    // todo 还没开始写段落样式
    public ParagraphProperty ParagraphProperty { set; get; }

    /// <summary>
    /// 段落管理器
    /// </summary>
    public ParagraphManager ParagraphManager { get; }

    /// <summary>
    /// 获取当前段落是文档的第几段，从0开始
    /// </summary>
    public int Index => ParagraphManager.GetParagraphIndex(this);

    private TextEditorCore TextEditor => ParagraphManager.TextEditor;

    /// <summary>
    /// 段落的字符管理
    /// </summary>
    private ParagraphCharDataManager CharDataManager { get; }

    //public IReadOnlyList<IImmutableRun> GetRunList() => TextRunList;

    public CharData GetCharData(ParagraphCharOffset offset)
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
    public IList<CharData>? SplitRemoveByParagraphOffset(ParagraphCaretOffset offset)
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

    public List<LineLayoutData> LineVisualDataList { get; } = new List<LineLayoutData>();

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