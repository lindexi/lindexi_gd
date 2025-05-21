using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落数据
/// </summary>
class ParagraphData : ITextParagraph
{
    public ParagraphData(IReadOnlyRunProperty paragraphStartRunProperty, ParagraphProperty paragraphProperty, ParagraphManager paragraphManager)
    {
        ParagraphProperty = paragraphProperty;
        ParagraphManager = paragraphManager;
        ParagraphStartRunProperty = paragraphStartRunProperty;

        CharDataManager = new ParagraphCharDataManager(this);
    }

    #region 布局

    public IParagraphLayoutData ParagraphLayoutData => _paragraphLayoutData;
    private readonly ParagraphLayoutData _paragraphLayoutData = new ParagraphLayoutData();

    public void SetParagraphLayoutTextSize(TextSize textSize)
        => _paragraphLayoutData.TextSize = textSize;

    public void SetParagraphLayoutOutlineSize(TextSize outlineSize)
        => _paragraphLayoutData.OutlineSize = outlineSize;

    public void SetParagraphLayoutContentThickness(TextThickness contentThickness)
        => _paragraphLayoutData.TextContentThickness = contentThickness;

    /// <summary>
    /// 设置段落的缩进信息
    /// </summary>
    /// <param name="indentInfo"></param>
    public void SetParagraphLayoutIndentInfo(in ParagraphLayoutIndentInfo indentInfo)
    {
        _paragraphLayoutData.IndentInfo = indentInfo;

        if (IsInDebugMode)
        {
            Verify();
        }

        return;

        void Verify()
        {
            ParagraphLayoutIndentInfo indentInfo = _paragraphLayoutData.IndentInfo;

            EqualAssets(ParagraphProperty.LeftIndentation, indentInfo.LeftIndentation, nameof(ParagraphProperty.LeftIndentation));
            EqualAssets(ParagraphProperty.RightIndentation, indentInfo.RightIndentation,
                nameof(ParagraphProperty.RightIndentation));
            EqualAssets(ParagraphProperty.Indent, indentInfo.Indent, nameof(ParagraphProperty.Indent));
            if (ParagraphProperty.IndentType != indentInfo.IndentType)
            {
                throw new TextEditorInnerDebugException($"对 IndentType 的预期和实际值不符。预期：{ParagraphProperty.IndentType}，实际：{indentInfo.IndentType}");
            }
         

            static void EqualAssets(double expect, double actual, string name)
            {
                if (Nearly.Equals(expect, actual) is false)
                {
                    throw new TextEditorInnerDebugException($"对 {name} 的预期和实际值不符。预期：{expect}，实际：{actual}");
                }
            }
        }
    }

    /// <summary>
    /// 设置布局数据是脏的
    /// </summary>
    public void SetLayoutDirty(bool exceptTextSize) => _paragraphLayoutData.SetLayoutDirty(exceptTextSize);

    /// <summary>
    /// 更新段落左上角起始点的坐标
    /// </summary>
    /// <param name="startPoint"></param>
    ///// <param name="outlineStartPoint"></param>
    public void UpdateParagraphLayoutStartPoint(TextPointInDocumentContentCoordinateSystem startPoint)
    {
        _paragraphLayoutData.StartPointInDocumentContentCoordinateSystem = startPoint;
        //_paragraphLayoutData.TextBounds = _paragraphLayoutData.TextBounds with
        //{
        //    X = textStartPoint.X,
        //    Y = textStartPoint.Y
        //};

        //_paragraphLayoutData.OutlineBounds = _paragraphLayoutData.OutlineBounds with
        //{
        //    X = outlineStartPoint.X, Y = outlineStartPoint.Y
        //};
    }

    #endregion

    /// <inheritdoc />
    public ParagraphProperty ParagraphProperty { private set; get; }

    /// <inheritdoc />
    public IReadOnlyRunProperty ParagraphStartRunProperty { get; private set; }

    public void SetParagraphProperty(ParagraphProperty paragraphProperty)
    {
        ParagraphProperty = paragraphProperty;

        // 段落属性变更，需要设置整段都是脏的
        SetDirty();
    }

    /// <summary>
    /// 更新段落的起始字符属性
    /// </summary>
    public void UpdateStartRunProperty()
    {
        if (CharDataManager.CharCount > 0)
        {
            CharData firstCharData = CharDataManager.GetCharData(0);
            IReadOnlyRunProperty runProperty = firstCharData.RunProperty;
            ParagraphStartRunProperty = runProperty;
        }
    }

    /// <summary>
    /// 段落管理器
    /// </summary>
    public ParagraphManager ParagraphManager { get; }

    /// <inheritdoc />
    public ParagraphIndex Index => ParagraphManager.GetParagraphIndex(this);

    private TextEditorCore TextEditor => ParagraphManager.TextEditor;

    public bool IsInDebugMode => TextEditor.IsInDebugMode;

    /// <summary>
    /// 段落的字符管理
    /// </summary>
    private ParagraphCharDataManager CharDataManager { get; }

    //public IReadOnlyList<IImmutableRun> GetRunList() => TextRunList;

    public CharData GetCharData(ParagraphCharOffset offset)
    {
        return CharDataManager.GetCharData(offset.Offset);
    }

    /// <summary>
    /// 获取这一段的换段符号
    /// </summary>
    /// <returns></returns>
    public CharData GetLineBreakCharData()
    {
        IReadOnlyRunProperty runProperty;
        if (IsEmptyParagraph)
        {
            // 如果这是一个空段，那就采用段落属性的字符属性
            runProperty = ParagraphStartRunProperty;
        }
        else
        {
            // 如果有内容，那就获取最后一个字符的字符样式
            runProperty = CharDataManager.GetCharData(CharCount - 1).RunProperty;
        }

        var charData = new CharData(LineBreakCharObject.Instance, runProperty);
        charData.CharLayoutData = new CharLayoutData(charData, this)
        {
            // 由于获取换行符是一个在任意逻辑执行的方法，此时也许是在布局过程中，如果设置或获取了字符尺寸等，那是不靠谱的
            // 创建 CharLayoutData 对象只是为了让属性不为空，不代表换行字符可获取正确的布局
            // 因此重新赋值为 0 的值，防止业务端使用字符坐标
            CurrentParagraphVersion = 0
        };
        return charData;
    }

    /// <inheritdoc />
    public TextReadOnlyListSpan<CharData> GetParagraphCharDataList() => ToReadOnlyListSpan(new ParagraphCharOffset(0));

    /// <summary>
    /// 获取字符列表
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    [Obsolete("这个方法存在的作用只是告诉你可以使用 ToReadOnlyListSpan 代替")]
    public TextReadOnlyListSpan<CharData> GetCharDataList(ParagraphCharOffset start, int length) =>
        ToReadOnlyListSpan(start, length);

    /// <summary>
    /// 获取此段落的字符列表
    /// </summary>
    /// <param name="start">相对于段落</param>
    /// <returns></returns>
    public TextReadOnlyListSpan<CharData> ToReadOnlyListSpan(ParagraphCharOffset start) =>
        CharDataManager.ToReadOnlyListSpan(start.Offset);

    /// <summary>
    /// 获取此段落的字符列表
    /// </summary>
    /// <param name="start"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public TextReadOnlyListSpan<CharData> ToReadOnlyListSpan(ParagraphCharOffset start, int length) =>
        CharDataManager.ToReadOnlyListSpan(start.Offset, length);

    /// <summary>
    /// 这一段的字符长度。不包括 \n 字符
    /// </summary>
    public int CharCount => CharDataManager.CharCount;

    /// <summary>
    /// 是否空段
    /// </summary>
    public bool IsEmptyParagraph => CharDataManager.CharCount == 0;

#if DEBUG
    /// <summary>
    /// 获取这个文本行是否已经从文档中删除
    /// </summary>
    /// 这个属性更多是给调试
    public bool IsDeleted { set; get; }
#endif

    /// <summary>
    /// 获取行分隔符的长度
    /// 0 为文档中的最后一行， 1 for <c>"\r"</c> or <c>"\n"</c> 但理论上文本库不会产生 \r 的内容
    /// 当本行被删除后这个属性值依然有效，这种情况下，它包含在删除之前的行分隔符的长度
    /// </summary>
    /// 无论在哪个平台上，都统一为 \n 一个字符
    public static int DelimiterLength => TextContext.NewLine.Length;

    /// <summary>
    /// 获取本文本行的起始位置在文档中的偏移量，此偏移量的计算考虑了换行符，如“123/r/n123”字符串，那么第二个段落的 Offset 为 "123".Length + DelimiterLength 的长度
    /// </summary>
    /// <exception cref="InvalidOperationException">这个文本行被删除后引发此异常</exception>
    public DocumentOffset GetParagraphStartOffset() => ParagraphManager.GetParagraphStartOffset(this);

    /// <summary>
    /// 将段落的光标坐标转换为文档光标坐标
    /// </summary>
    /// <param name="paragraphCaretOffset"></param>
    /// <returns></returns>
    public CaretOffset ToCaretOffset(ParagraphCaretOffset paragraphCaretOffset) =>
        new CaretOffset(paragraphCaretOffset.Offset + GetParagraphStartOffset());

    /// <summary>
    /// 将段落字符坐标转换为文档坐标
    /// </summary>
    /// <param name="paragraphCharOffset"></param>
    /// <returns></returns>
    public DocumentOffset ToDocumentOffset(ParagraphCharOffset paragraphCharOffset) =>
        new DocumentOffset(paragraphCharOffset.Offset + GetParagraphStartOffset());

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

    public void RemoveRange(ParagraphCaretOffset start) => RemoveRange(start, CharCount - start.Offset);

    public void RemoveRange(ParagraphCaretOffset start, int count) => CharDataManager.RemoveRange(start.Offset, count);

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
            throw new ArgumentOutOfRangeException(nameof(offset), $"段落字符:{CharCount};参数Offset={offset.Offset}");
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

    public void AppendRun(IImmutableRun run, IReadOnlyRunProperty runProperty)
    {
        //var runProperty = run.RunProperty ??
        //                  ParagraphProperty.ParagraphStartRunProperty ?? TextEditor.DocumentManager.CurrentRunProperty;

        //TextRunList.Add(run);
        for (int i = 0; i < run.Count; i++)
        {
            var charObject = run.GetChar(i).DeepClone();
            IPlatformRunPropertyCreator platformRunPropertyCreator = TextEditor.PlatformProvider.GetPlatformRunPropertyCreator();
            IReadOnlyRunProperty platformRunProperty =
                platformRunPropertyCreator.ToPlatformRunProperty(charObject, runProperty);

            // 似乎对每个 Char 都调用也不亏，正常都是相同的 runProperty 对象，除非字体不存在等情况
            //// 不应该为每个 Char 都调用一次 ToPlatformRunProperty 防止创建出大量相同的字符属性对象

            var charData = new CharData(charObject, platformRunProperty);
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

    /// <summary>
    /// 将 <paramref name="charData"/> 加入到段落。记得调用完成，自己调用 <see cref="SetDirty"/> 方法
    /// </summary>
    /// <param name="charData"></param>
    /// <exception cref="ArgumentException"></exception>
    internal void AppendCharData(CharData charData)
    {
        // 谨慎 CharData 的加入开放给框架之外，原因在于 CharData 不能被加入到文本多次
        // 一个 CharData 只能存在一个文本，且只存在一次。原因是 CharData 里面包含了渲染布局信息
        // 加入多次将会让布局很乱
        if (charData.CharLayoutData is not null)
        {
            throw new ArgumentException($"此 CharData 已经被加入到某个段落，不能重复加入");
        }

        if (charData.IsLineBreakCharData)
        {
            throw new ArgumentException($"禁止将 {nameof(LineBreakCharObject)} 加入到段落");
        }

        CharDataManager.Add(charData);
    }

    internal void AppendCharData(IEnumerable<CharData> charDataList)
    {
        CharDataManager.AddRange(charDataList);
    }

    #region 渲染排版数据

    public List<LineLayoutData> LineLayoutDataList { get; } = new List<LineLayoutData>();

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

    /// <inheritdoc />
    public override string ToString()
    {
        return $"第{Index.Index}段 {GetText()}";
    }
}
