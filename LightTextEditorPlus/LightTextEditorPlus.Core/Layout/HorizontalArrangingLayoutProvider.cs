using System;
using System.Collections.Generic;
using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.WordDividers;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 水平方向布局的提供器
/// </summary>
class HorizontalArrangingLayoutProvider : ArrangingLayoutProvider, IInternalCharDataSizeMeasurer
{
    public HorizontalArrangingLayoutProvider(LayoutManager layoutManager) : base(layoutManager)
    {
        _divider = new DefaultWordDivider(layoutManager.TextEditor, this);
    }

    public override ArrangingType ArrangingType => ArrangingType.Horizontal;

    #region 02 预布局阶段

    #region 更新非脏的段落和行

    protected override ParagraphLayoutResult UpdateParagraphStartPoint(in ParagraphLayoutArgument argument)
    {
        var paragraph = argument.ParagraphData;

        // 先设置是脏的，然后再更新，这样即可更新段落版本号
        paragraph.SetDirty();

        double paragraphBefore = argument.IsFirstParagraph ? 0 /*首段不加段前距离*/  : paragraph.ParagraphProperty.ParagraphBefore;
        var currentStartPoint = argument.CurrentStartPoint with
        {
            Y = argument.CurrentStartPoint.Y + paragraphBefore
        };
        // todo 对于 Outline 来说，ParagraphBefore 应该被忽略，且应该被加入到 Outline 里面
        paragraph.UpdateParagraphLayoutStartPoint(currentStartPoint, currentStartPoint);

        var layoutArgument = argument with
        {
            CurrentStartPoint = currentStartPoint
        };

        var nextLineStartPoint = UpdateParagraphLineLayoutDataStartPoint(layoutArgument);
        // 设置当前段落已经布局完成
        paragraph.SetFinishLayout();

        return new ParagraphLayoutResult(nextLineStartPoint);
    }

    /// <summary>
    /// 更新段落里面的所有行的起始点
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private TextPoint UpdateParagraphLineLayoutDataStartPoint(in ParagraphLayoutArgument argument)
    {
        var currentStartPoint = argument.CurrentStartPoint;
        var paragraph = argument.ParagraphData;

        foreach (LineLayoutData lineVisualData in paragraph.LineLayoutDataList)
        {
            UpdateLineLayoutDataStartPoint(lineVisualData, currentStartPoint);

            currentStartPoint = GetNextLineStartPoint(currentStartPoint, lineVisualData);
        }

        return currentStartPoint;
    }

    /// <summary>
    /// 重新更新每一行的起始点。例如现在第一段的文本加了一行，那第二段的所有文本都需要更新每一行的起始点，而不需要重新布局第二段
    /// </summary>
    /// <param name="lineLayoutData"></param>
    /// <param name="startPoint"></param>
    void UpdateLineLayoutDataStartPoint(LineLayoutData lineLayoutData, TextPoint startPoint)
    {
        // 更新包括两个方面：
        // 1. 此行的起点
        // 2. 此行内的所有字符的起点坐标
        lineLayoutData.CharStartPoint = startPoint;

        // 更新行内所有字符的坐标
        TextReadOnlyListSpan<CharData> list = lineLayoutData.GetCharList();

        var lineHeight = lineLayoutData.LineContentSize.Height;
        UpdateTextLineStartPoint(list, startPoint, lineHeight,
            // 这里只是更新行的起始点，行内的字符坐标不需要变更，因此不需要重新排列 X 坐标
            reArrangeXPosition: false,
            needUpdateCharLayoutDataVersion: true);

        lineLayoutData.UpdateVersion();
    }

    #endregion

    #region LayoutParagraphCore

    /// <summary>
    /// 布局段落的核心逻辑
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="startParagraphOffset">开始布局的字符，刚好是一行的开始</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <remarks>
    /// 逻辑上是：
    /// 布局按照： 文本-段落-行-Run-字符
    /// 布局整个文本
    /// 布局文本的每个段落 <see cref="LayoutParagraphCore"/>
    /// 段落里面，需要对每一行进行布局 <see cref="LayoutWholeLine"/>
    /// 每一行里面，需要对每个 Char 字符进行布局 <see cref="LayoutSingleCharInLine"/>
    /// 每个字符需要调用平台的测量 <see cref="ArrangingLayoutProvider.MeasureCharInfo"/>
    /// </remarks>
    protected override ParagraphLayoutResult LayoutParagraphCore(in ParagraphLayoutArgument argument,
        in ParagraphCharOffset startParagraphOffset)
    {
        var paragraph = argument.ParagraphData;
        // 先更新非脏的行的坐标
        // 布局左上角坐标
        TextPoint currentStartPoint;
        // 根据是否存在缓存行决定是否需要计算段前距离
        if (paragraph.LineLayoutDataList.Count == 0)
        {
            // 一行都没有的情况下，需要计算段前距离
            double paragraphBefore = argument.IsFirstParagraph ? 0 /*首段不加段前距离*/  : paragraph.ParagraphProperty.ParagraphBefore;

            currentStartPoint = argument.CurrentStartPoint with
            {
                Y = argument.CurrentStartPoint.Y + paragraphBefore
            };
        }
        else
        {
            // 有缓存的行，证明段落属性没有更改，不需要计算段前距离
            // 只需要更新缓存的行
            currentStartPoint = UpdateParagraphLineLayoutDataStartPoint(argument);
        }

        // 预布局过程中，不考虑边距的影响。但只考虑缩进等对可用尺寸的影响
        // 在回溯过程中，才赋值给到边距。详细请参阅 《文本库行布局信息定义.enbx》 维护文档
        //// 更新段左边距
        //currentStartPoint = currentStartPoint with
        //{
        //    X = paragraph.ParagraphProperty.LeftIndentation
        //};

        // 如果是空段的话，那就进行空段布局，否则布局段落里面每一行
        bool isEmptyParagraph = paragraph.CharCount == 0;
        if (isEmptyParagraph)
        {
            // 空段布局
            currentStartPoint = LayoutEmptyParagraph(argument, currentStartPoint);
        }
        else
        {
            // 布局段落里面每一行
            currentStartPoint = LayoutParagraphLines(argument, startParagraphOffset, currentStartPoint);
        }

        //// 考虑行复用，例如刚好添加的内容是一行。或者在一行内做文本替换等
        //// 这个没有啥优先级。测试了 SublimeText 和 NotePad 工具，都没有做此复用，预计有坑
        
        // 下一段的距离需要加上段后距离
        double paragraphAfter =
            argument.IsLastParagraph ? 0 /*最后一段不加段后距离*/ : paragraph.ParagraphProperty.ParagraphAfter;
        var nextLineStartPoint = currentStartPoint with
        {
            Y = currentStartPoint.Y + paragraphAfter,
        };

        // 计算段落的文本范围
        TextPoint paragraphTextStartPoint = argument.ParagraphData.LineLayoutDataList[0].CharStartPoint;
        TextSize paragraphTextSize = BuildParagraphSize(argument);
        TextRect paragraphTextBounds = new TextRect(paragraphTextStartPoint, paragraphTextSize);
        paragraph.SetParagraphLayoutTextBounds(paragraphTextBounds);
       
        return new ParagraphLayoutResult(nextLineStartPoint);
    }

    /// <summary>
    /// 布局空段
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="currentStartPoint"></param>
    /// <returns></returns>
    private TextPoint LayoutEmptyParagraph(in ParagraphLayoutArgument argument, TextPoint currentStartPoint)
    {
        var paragraph = argument.ParagraphData;
        // 如果是空段的话，如一段只是一个 \n 而已，那就需要执行空段布局逻辑
        Debug.Assert(paragraph.LineLayoutDataList.Count == 0, "空段布局时一定是一行都不存在");
        var emptyParagraphLineHeightMeasureResult = MeasureEmptyParagraphLineHeight(
            new EmptyParagraphLineHeightMeasureArgument(paragraph.ParagraphProperty, argument.ParagraphIndex));
        double lineHeight = emptyParagraphLineHeightMeasureResult.LineHeight;

        // 加上空行
        var lineLayoutData = new LineLayoutData(paragraph)
        {
            CharStartParagraphIndex = 0,
            CharEndParagraphIndex = 0,
            CharStartPoint = currentStartPoint,
            LineContentSize = new TextSize(0, lineHeight)
        };
        paragraph.LineLayoutDataList.Add(lineLayoutData);

        currentStartPoint = currentStartPoint with
        {
            Y = currentStartPoint.Y + lineHeight
        };
        return currentStartPoint;
    }

    /// <summary>
    /// 布局段落里面的每一行
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="startParagraphOffset"></param>
    /// <param name="currentStartPoint"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="TextEditorDebugException"></exception>
    /// <exception cref="TextEditorInnerException"></exception>
    private TextPoint LayoutParagraphLines(in ParagraphLayoutArgument argument, in ParagraphCharOffset startParagraphOffset,
        TextPoint currentStartPoint)
    {
        ParagraphData paragraph = argument.ParagraphData;
        // 获取最大宽度信息
        double lineMaxWidth = GetLineMaxWidth();

        var wholeRunLineLayouter = TextEditor.PlatformProvider.GetWholeRunLineLayouter();
        for (var i = startParagraphOffset.Offset; i < paragraph.CharCount;)
        {
            // 开始行布局
            // 第一个 Run 就是行的开始
            TextReadOnlyListSpan<CharData> charDataList = paragraph.ToReadOnlyListSpan(new ParagraphCharOffset(i));

            if (TextEditor.IsInDebugMode)
            {
                // 这是调试代码，判断是否在布局过程，漏掉某个字符
                foreach (var charData in charDataList)
                {
                    charData.IsSetStartPointInDebugMode = false;
                }
            }

            int lineIndex = paragraph.LineLayoutDataList.Count;
            var isFirstLine = lineIndex == 0;
            ParagraphProperty paragraphProperty = paragraph.ParagraphProperty;
            double leftIndentationValue = paragraphProperty.GetLeftIndentationValue(isFirstLine);
            // 一行可用的宽度，需要减去缩进
            var usableLineMaxWidth = lineMaxWidth - leftIndentationValue - paragraphProperty.RightIndentation;

            WholeLineLayoutResult result;
            var wholeRunLineLayoutArgument = new WholeLineLayoutArgument(argument.ParagraphIndex,
                lineIndex, paragraphProperty, charDataList,
                usableLineMaxWidth, currentStartPoint, argument.UpdateLayoutContext);
            if (wholeRunLineLayouter != null)
            {
                result = wholeRunLineLayouter.LayoutWholeLine(wholeRunLineLayoutArgument);
            }
            else
            {
                // 继续往下执行，如果没有注入自定义的行布局层的话，则使用默认的行布局器
                // 为什么不做默认实现？因为默认实现会导致默认逻辑写在外面，而不是相同一个文件里面，没有内聚性，对于文本排版布局内部重调试的情况下，不友好。即，尽管代码结构是清晰了，但实际调试体验却下降了，一个调试或阅读代码需要跳转多个文件，复杂度提升
                result = LayoutWholeLine(wholeRunLineLayoutArgument);
            }

            // 当前的行布局信息
            var currentLineLayoutData = new LineLayoutData(paragraph)
            {
                CharStartParagraphIndex = i,
                CharEndParagraphIndex = i + result.CharCount,
                LineContentSize = result.LineSize,
                LineCharTextSize = result.TextSize,
                CharStartPoint = currentStartPoint,
            };
            // 更新字符信息
            Debug.Assert(result.CharCount <= charDataList.Count, "所获取的行的字符数量不能超过可提供布局的行的字符数量");
            for (var index = 0; index < result.CharCount; index++)
            {
                var charData = charDataList[index];

                if (TextEditor.IsInDebugMode)
                {
                    if (charData.IsSetStartPointInDebugMode == false)
                    {
                        throw new TextEditorDebugException($"存在某个字符没有在布局时设置坐标",
                            (charData, currentLineLayoutData, i + index));
                    }
                }

                Debug.Assert(charData.CharLayoutData != null, "经过行布局，字符存在行布局信息");
                charData.CharLayoutData!.CharIndex = new ParagraphCharOffset(i + index);
                charData.CharLayoutData.CurrentLine = currentLineLayoutData;
                charData.CharLayoutData.UpdateVersion();
            }

            paragraph.LineLayoutDataList.Add(currentLineLayoutData);

            i += result.CharCount;

            if (result.CharCount == 0)
            {
                // todo 理论上不可能，表示行布局出错了
                // 支持文本宽度小于一个字符的宽度的布局
                throw new TextEditorInnerException($"某一行在布局时，只采用了零个字符");
            }

            currentStartPoint = GetNextLineStartPoint(currentStartPoint, currentLineLayoutData);
        }

        return currentStartPoint;
    }

    #endregion

    #region LayoutWholeLine 布局一行的字符

    /// <summary>
    /// 布局一行的字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// 1. 布局一行的字符，分行算法
    /// 2. 处理行高，行距算法
    private WholeLineLayoutResult LayoutWholeLine(in WholeLineLayoutArgument argument)
    {
        var charDataList = argument.CharDataList;
        var currentStartPoint = argument.CurrentStartPoint;

        if (charDataList.Count == 0)
        {
            // 理论上不会进入这里，如果没有任何的字符，那就不需要进行行布局
            return new WholeLineLayoutResult(TextSize.Zero, TextSize.Zero, 0);
        }

        var layoutResult = LayoutWholeLineChars(argument);
#if DEBUG
        if (layoutResult.CurrentLineCharTextSize.Width > 0 && layoutResult.CurrentLineCharTextSize.Height == 0)
        {
            // 这可能是不正常的情况
            // 可以在这里打断点看
            TextEditor.Logger.LogDebug($"单行布局结果是有宽度没高度，预计是不正确的情况。仅调试下输出");
        }
#endif

        int wholeCharCount = layoutResult.WholeCharCount;
        TextSize currentTextSize = layoutResult.CurrentLineCharTextSize;

        if (wholeCharCount == 0)
        {
            // 这一行一个字符都不能拿
            Debug.Assert(currentTextSize == TextSize.Zero);
            // 有可能这一行一个字符都不能拿，但是还是有行高的
            var currentLineSize = currentTextSize; // todo 这里需要重新确认一下，行高应该是多少
            return new WholeLineLayoutResult(currentLineSize, currentTextSize, wholeCharCount);
        }

        // 遍历一次，用来取出其中 FontSize 最大的字符，此字符的对应字符属性就是所期望的参与后续计算的字符属性
        // 遍历这一行的所有字符，找到最大字符的字符属性
        var charDataTakeList = charDataList.Slice(0, wholeCharCount);
        CharData maxFontSizeCharData = GetMaxFontSizeCharData(charDataTakeList);
        IReadOnlyRunProperty maxFontSizeCharRunProperty = maxFontSizeCharData.RunProperty;

        // 处理行距
        var lineSpacingCalculateArgument = new LineSpacingCalculateArgument(argument.ParagraphIndex, argument.LineIndex, argument.ParagraphProperty, maxFontSizeCharRunProperty);
        LineSpacingCalculateResult lineSpacingCalculateResult = CalculateLineSpacing(lineSpacingCalculateArgument);

        // 获取到行高，行高的意思是整行的高度，包括空白行距+字符高度
        double lineHeight = lineSpacingCalculateResult.TotalLineHeight;
        if (lineSpacingCalculateResult.ShouldUseCharLineHeight)
        {
            lineHeight = currentTextSize.Height;
        }
        
        var lineSpacing = lineHeight - currentTextSize.Height; // 行距值，现在仅调试用途
        GC.KeepAlive(lineSpacing);
        // 不能使用 lineSpacing 作为计算参考，因为在 Skia 平台下 TextSize 会更大，超过了布局行高的值，导致 lineSpacing 为负数

        // 具体设置每个字符的坐标的逻辑
        UpdateTextLineStartPoint(charDataTakeList, currentStartPoint, lineHeight,
            // 重新排列 X 坐标，因为这一行的字符可能是新加入的或修改的，需要重新排列 X 坐标
            reArrangeXPosition: true,
            // 这时候不需要更新 CharLayoutData 版本，因为这个版本是在布局行的时候更新的
            // 这时候必定这一行需要重新更新渲染，不需要标记脏，这是新加入的布局行
            needUpdateCharLayoutDataVersion: false);

        // 行的尺寸
        var lineSize = new TextSize(currentTextSize.Width, lineHeight);

        return new WholeLineLayoutResult(lineSize, currentTextSize, wholeCharCount);
    }

    /// <summary>
    /// 布局一行的结果
    /// </summary>
    /// <param name="CurrentLineCharTextSize"></param>
    /// <param name="WholeCharCount"></param>
    readonly record struct WholeLineCharsLayoutResult(TextSize CurrentLineCharTextSize, int WholeCharCount);

    /// <summary>
    /// 布局一行里面有哪些字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private WholeLineCharsLayoutResult LayoutWholeLineChars(in WholeLineLayoutArgument argument)
    {
        var (paragraphIndex, lineIndex, paragraphProperty, charDataList, lineMaxWidth, currentStartPoint, context) = argument;

#if DEBUG
        // 调试下显示当前这一行的文本，方便了解当前在哪一行
        string currentLineText = argument.DebugText;
        GC.KeepAlive(currentLineText);
#endif

        var singleRunLineLayouter = TextEditor.PlatformProvider.GetSingleRunLineLayouter();

        // RunLineMeasurer
        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        // 当前相对于 charDataList 的当前序号
        int currentIndex = 0;
        // 当前的字符布局尺寸
        var currentSize = TextSize.Zero;

        while (currentIndex < charDataList.Count)
        {
            // 一行里面需要逐个字符进行布局
            var arguments = new SingleCharInLineLayoutArgument(charDataList, currentIndex, lineRemainingWidth,
                paragraphProperty, context);

            SingleCharInLineLayoutResult result;
            if (singleRunLineLayouter is not null)
            {
                result = singleRunLineLayouter.LayoutSingleCharInLine(arguments);

#if DEBUG
                if (result.TotalSize.Width > 0 && result.TotalSize.Height == 0)
                {
                    // 这可能是不正常的情况
                    // 可以在这里打断点看
                    TextEditor.Logger.LogDebug($"单行布局结果是有宽度没高度，预计是不正确的情况。仅调试下输出");
                }
#endif
            }
            else
            {
                result = LayoutSingleCharInLine(arguments);
            }

            if (result.CanTake)
            {
                currentSize = currentSize.HorizontalUnion(result.TotalSize);

                currentIndex += result.TakeCount;

                // 计算此行剩余的宽度
                lineRemainingWidth -= result.TotalSize.Width;
            }

            if (result.ShouldBreakLine)
            {
                // 换行，这一行布局完成
                break;
            }
        }

        // 整个行所使用的字符数量
        var wholeCharCount = currentIndex;
        return new WholeLineCharsLayoutResult(currentSize, wholeCharCount);
    }

    /// <summary>
    /// 获取给定行的最大字号的字符属性。这个属性就是这一行的代表属性
    /// </summary>
    /// <param name="charDataList"></param>
    /// <returns></returns>
    private static CharData GetMaxFontSizeCharData(in TextReadOnlyListSpan<CharData> charDataList)
    {
        CharData firstCharData = charDataList[0];
        var maxFontSizeCharData = firstCharData;
        // 遍历这一行的所有字符，找到最大字符的字符属性
        for (var i = 1; i < charDataList.Count; i++)
        {
            var charData = charDataList[i];
            if (charData.RunProperty.FontSize > maxFontSizeCharData.RunProperty.FontSize)
            {
                maxFontSizeCharData = charData;
            }
        }

        return maxFontSizeCharData;
    }

    /// <summary>
    /// 更新行的起始点
    /// </summary>
    /// <param name="lineCharList"></param>
    /// <param name="startPoint">文档布局给到行的距离文本框开头的距离</param>
    /// <param name="lineHeight">当前字符所在行的行高，包括行距在内</param>
    /// <param name="reArrangeXPosition">是否需要重新排列 X 坐标。对于只是更新行的 Y 坐标的情况，是不需要重新排列的</param>
    /// <param name="needUpdateCharLayoutDataVersion">是否需要更新 CharLayoutData 版本</param>
    private void UpdateTextLineStartPoint(TextReadOnlyListSpan<CharData> lineCharList, TextPoint startPoint,
        double lineHeight, bool reArrangeXPosition, bool needUpdateCharLayoutDataVersion)
    {
        var lineTop = startPoint.Y; // 相对于文本框的坐标
        var currentX = startPoint.X;

        CharData maxFontSizeCharData = GetMaxFontSizeCharData(lineCharList);
        // 求基线的行内偏移量
        var maxFontHeight = maxFontSizeCharData.Size!.Value.Height;
        // 先算行内坐标，再转文档坐标
        double maxFontYOffset = lineHeight - maxFontHeight; 

        // 计算字符在行内的坐标
        // 字符在行内的垂直对齐方式
        RatioVerticalCharInLineAlignment verticalCharInLineAlignment = TextEditor.LineSpacingConfiguration.VerticalCharInLineAlignment;
        // 按照比例对齐
        maxFontYOffset = maxFontYOffset * verticalCharInLineAlignment.LineSpaceRatio;

        // 计算出最大字符的基线坐标
        maxFontYOffset += maxFontSizeCharData.Baseline;
        // 从行内坐标转换为文档坐标
        maxFontYOffset += lineTop; // 相对于文本框的坐标

        foreach (CharData charData in lineCharList)
        {
            // 计算和更新每个字符的相对文本框的坐标
            Debug.Assert(charData.Size != null, "charData.LineCharSize != null");
            var charDataSize = charData.Size!.Value;

            double xOffset;
            if (reArrangeXPosition)
            {
                // 保持 X 不变
                xOffset = currentX;
            }
            else
            {
                xOffset = charData.CharLayoutData!.CharLineStartPoint.X;
            }

            // 通过将最大字符的基线和当前字符的基线的差，来计算出当前字符的偏移量
            // 如此可以实现字体的基线对齐
            double yOffset = maxFontYOffset - charData.Baseline;

            charData.SetLayoutStartPoint(new TextPoint(xOffset, yOffset)/*, new TextPoint(xOffset, yOffset)*/);

            if (needUpdateCharLayoutDataVersion)
            {
                charData.CharLayoutData.UpdateVersion();
            }

            currentX += charDataSize.Width;
        }
    }

    #endregion  

    /// <summary>
    /// 布局一行里面的单个字符
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    private SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // LayoutRule 布局规则
        // 可选无规则-直接字符布局，预计没有人使用
        // 调用分词规则-支持注入分词规则

        // 使用分词规则进行布局
        bool useWordDividerLayout = true;

        if (useWordDividerLayout)
        {
            return _divider.LayoutSingleCharInLine(argument);
        }
        else
        {
            var charData = argument.CurrentCharData;

            TextSize textSize = GetCharSize(charData);

            // 单个字符直接布局，无视语言文化。快，但是诡异
            if (argument.LineRemainingWidth > textSize.Width)
            {
                return new SingleCharInLineLayoutResult(takeCount: 1, textSize);
            }
            else
            {
                // 如果尺寸不足，也就是一个都拿不到
                return new SingleCharInLineLayoutResult(takeCount: 0, default);
            }
        }
    }

    /// <summary>
    /// 分词器
    /// </summary>
    private readonly DefaultWordDivider _divider;

    #region 辅助方法

    [DebuggerStepThrough] // 别跳太多层
    TextSize IInternalCharDataSizeMeasurer.GetCharSize(CharData charData) => GetCharSize(charData);

    /// <summary>
    /// 获取给定字符的尺寸
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    private TextSize GetCharSize(CharData charData)
    {
        // 字符可能自己缓存有了自己的尺寸，如果有缓存，那是可以重复使用
        var cacheSize = charData.Size;

        TextSize textSize;
        if (cacheSize == null)
        {
            var charInfo = new CharInfo(charData.CharObject, charData.RunProperty);
            CharInfoMeasureResult charInfoMeasureResult;
            ICharInfoMeasurer? charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();
            if (charInfoMeasurer != null)
            {
                charInfoMeasureResult = charInfoMeasurer.MeasureCharInfo(charInfo);
            }
            else
            {
                charInfoMeasureResult = MeasureCharInfo(charInfo);
            }

            textSize = charInfoMeasureResult.Bounds.TextSize;
            charData.SetCharDataInfo(textSize, charInfoMeasureResult.Baseline);
        }
        else
        {
            textSize = cacheSize.Value;
        }

        return textSize;
    }

    /// <summary>
    /// 获取下一行的起始点
    /// </summary>
    /// 对于横排布局来说，只是更新 Y 值即可
    /// <param name="currentStartPoint"></param>
    /// <param name="currentLineLayoutData"></param>
    /// <returns></returns>
    private static TextPoint GetNextLineStartPoint(TextPoint currentStartPoint, LineLayoutData currentLineLayoutData)
    {
        currentStartPoint = new TextPoint(currentStartPoint.X, currentStartPoint.Y + currentLineLayoutData.LineContentSize.Height);
        return currentStartPoint;
    }

    private static TextSize BuildParagraphSize(in ParagraphLayoutArgument argument)
    {
        var paragraphSize = new TextSize(0, 0);
        foreach (var lineVisualData in argument.ParagraphData.LineLayoutDataList)
        {
            var width = Math.Max(paragraphSize.Width, lineVisualData.LineContentSize.Width);
            var height = paragraphSize.Height + lineVisualData.LineContentSize.Height;

            paragraphSize = new TextSize(width, height);
        }

        return paragraphSize;
    }

    /// <summary>
    /// 获取行的最大宽度
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private double GetLineMaxWidth()
    {
        double lineMaxWidth = TextEditor.SizeToContent switch
        {
            TextSizeToContent.Manual => TextEditor.DocumentManager.DocumentWidth,
            TextSizeToContent.Width => double.PositiveInfinity,
            TextSizeToContent.Height => TextEditor.DocumentManager.DocumentWidth,
            TextSizeToContent.WidthAndHeight => double.PositiveInfinity,
            _ => throw new ArgumentOutOfRangeException()
        };
        return lineMaxWidth;
    }

    #endregion

    protected override TextPoint GetNextParagraphLineStartPoint(ParagraphData paragraphData)
    {
        const double x = 0;
        var layoutData = paragraphData.ParagraphLayoutData;
        TextRect textBounds = layoutData.TextBounds;
        var y = textBounds.Y + textBounds.Height;
        return new TextPoint(x, y);

        // 以下是通过最后一行的值进行计算的。不足的是需要判断空段，因此不如使用段落偏移加上段落高度进行计算
        //if (paragraphData.LineVisualDataList.Count == 0)
        //{
        //    // 这一段没有包含任何的行。这一段里面可能没有任何文本，只是一个 \r\n 的空段
        //    Debug.Assert(paragraphData.CharCount == 0,"只有空段才没有包含行");

        //    const double x = 0;
        //    var layoutData = paragraphData.ParagraphLayoutData;
        //    var y = layoutData.CharStartPoint.Y + layoutData.LineCharSize.Height;
        //    return new Point(x, y);
        //}
        //else
        //{
        //    var lineVisualData = paragraphData.LineVisualDataList.Last();
        //    const double x = 0;
        //    var y = lineVisualData.CharStartPoint.Y + lineVisualData.LineCharSize.Height;
        //    return new Point(x, y);
        //}
    }

    #endregion 02 预布局阶段

    #region 03 回溯最终布局阶段

    protected override void FinalLayoutDocument(PreLayoutDocumentResult preLayoutDocumentResult, UpdateLayoutContext updateLayoutContext)
    {
        updateLayoutContext.RecordDebugLayoutInfo($"FinalLayoutDocument 进入最终布局阶段");

        TextRect documentBounds = preLayoutDocumentResult.DocumentBounds;
        var documentWidth = CalculateHitBounds(documentBounds).Width;
        IReadOnlyList <ParagraphData> paragraphList = updateLayoutContext.ParagraphList;

        for (var paragraphIndex = 0/*为什么从首段开始？如右对齐情况下，被撑大文档范围，则即使没有变脏也需要更新坐标*/; paragraphIndex < paragraphList.Count; paragraphIndex++)
        {
            updateLayoutContext.RecordDebugLayoutInfo($"开始回溯第 {paragraphIndex} 段");

            ParagraphData paragraph = paragraphList[paragraphIndex];
            ParagraphProperty paragraphProperty = paragraph.ParagraphProperty;

            IParagraphLayoutData layoutData = paragraph.ParagraphLayoutData;

            for (int lineIndex = 0; lineIndex < paragraph.LineLayoutDataList.Count; lineIndex++)
            {
                updateLayoutContext.RecordDebugLayoutInfo($"开始回溯第 {paragraphIndex} 段的第 {lineIndex} 行");

                var isFirstLine = lineIndex == 0;
                // 是否最后一行
                var isLastLine = lineIndex == paragraph.LineLayoutDataList.Count - 1;

                LineLayoutData lineLayoutData = paragraph.LineLayoutDataList[lineIndex];
                HorizontalTextAlignment horizontalTextAlignment = paragraphProperty.HorizontalTextAlignment;

                // 空白的宽度
                var gapWidth = documentWidth - lineLayoutData.LineContentSize.Width;
                double leftIndentation;
                if (paragraphProperty.IndentType == IndentType.FirstLine)
                {
                    if (isFirstLine)
                    {
                        leftIndentation = paragraphProperty.LeftIndentation;
                    }
                    else
                    {
                        // 首行缩进下非首行，左缩进为 0
                        leftIndentation = 0;
                    }
                }
                else
                {
                    leftIndentation = paragraphProperty.LeftIndentation;
                }

                var indentationThickness =
                    new TextThickness(leftIndentation, 0, paragraphProperty.RightIndentation, 0);

                // 可用的空白宽度。即空白宽度减去左缩进和右缩进
                double usableGapWidth = gapWidth - leftIndentation - paragraphProperty.RightIndentation;

                TextThickness horizontalTextAlignmentGapThickness;
                if (horizontalTextAlignment == HorizontalTextAlignment.Left)
                {
                    horizontalTextAlignmentGapThickness = new TextThickness(0, 0, usableGapWidth, 0);
                }
                else if (horizontalTextAlignment == HorizontalTextAlignment.Center)
                {
                    horizontalTextAlignmentGapThickness = new TextThickness(usableGapWidth / 2, 0, usableGapWidth/2, 0);
                }
                else if (horizontalTextAlignment == HorizontalTextAlignment.Right)
                {
                    horizontalTextAlignmentGapThickness = new TextThickness(usableGapWidth, 0, 0, 0);
                }
                else
                {
                    // 两端对齐 还不知道如何实现
                    throw new NotSupportedException($"不支持 {horizontalTextAlignment} 对齐方式");
                }

                lineLayoutData
                    .SetLineFinalLayoutInfo(indentationThickness, horizontalTextAlignmentGapThickness);

                {
                    // 计算 Outline 的范围
                    var x = 0;
                    var y = lineLayoutData.CharStartPoint.Y;
                    var width = documentWidth;
                    var height = lineLayoutData.LineContentSize.Height;

                    lineLayoutData.OutlineBounds = new TextRect(x, y, width, height);
                }
            }

            // 给定段落的范围
            paragraph.SetParagraphLayoutOutlineBounds(layoutData.TextBounds with
            {
                X = 0,
                Width = documentWidth
            });

            updateLayoutContext.RecordDebugLayoutInfo($"完成回溯第 {paragraphIndex} 段");

            // 设置当前段落已经布局完成
            paragraph.SetFinishLayout();
        }

        updateLayoutContext.RecordDebugLayoutInfo($"FinalLayoutDocument 完成最终布局阶段");
    }
    #endregion 03 回溯最终布局阶段

    #region 通用辅助方法

    /// <inheritdoc />
    protected override TextRect CalculateHitBounds(in TextRect documentBounds)
    {
        // 获取当前文档的大小，对水平布局来说，只取横排的最大值即可
        double lineMaxWidth = GetLineMaxWidth();
        var documentWidth = lineMaxWidth;
        if (!double.IsFinite(documentWidth))
        {
            // 非有限宽度，则采用文档的宽度
            documentWidth = documentBounds.Width;
        }

        // 高度不改变。这样即可让命中的时候，命中到文档下方时，命中可计算超过命中范围
        return documentBounds with { Width = documentWidth };
    }

    #endregion 通用辅助方法
}
