using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 光标下的渲染信息
/// </summary>
public readonly struct CaretRenderInfo
{
    internal CaretRenderInfo(TextEditorCore textEditor, int lineIndex, LineCharOffset hitLineCharOffset,
        LineCaretOffset hitLineCaretOffset, ParagraphCaretOffset hitOffset, CaretOffset caretOffset,
        LineLayoutData lineLayoutData, bool isHitLineEnd)
    {
        TextEditor = textEditor;
        LineIndex = lineIndex;
        HitLineCharOffset = hitLineCharOffset;
        HitLineCaretOffset = hitLineCaretOffset;
        HitOffset = hitOffset;
        CaretOffset = caretOffset;
        LineLayoutData = lineLayoutData;
        IsLineEnd = isHitLineEnd;

        if (textEditor.IsInDebugMode)
        {
            CharData? currentCharData = CharData;
            GC.KeepAlive(currentCharData);
        }
    }

    internal TextEditorCore TextEditor { get; }

    /// <summary>
    /// 行在段落里的序号
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// 段落在文档里属于第几段
    /// </summary>
    public ParagraphIndex ParagraphIndex => ParagraphData.Index;

    /// <summary>
    /// 这一行的字符列表
    /// </summary>
    public TextReadOnlyListSpan<CharData> LineCharDataList => LineLayoutData.GetCharList();

    /// <summary>
    /// 行的范围
    /// </summary>
    public TextRect LineBounds => LineLayoutData.GetLineContentBounds();

    /// <summary>
    /// 命中到行的哪个字符
    /// </summary>
    public LineCharOffset HitLineCharOffset { get; }

    /// <summary>
    /// 命中到行的哪个光标
    /// </summary>
    public LineCaretOffset HitLineCaretOffset { get; }

    /// <summary>
    /// 是否命中到行的起点
    /// </summary>
    public bool IsLineStart => CaretOffset.IsAtLineStart;

    /// <summary>
    /// 命中的字符。如果是空段，那将没有命中哪个字符。对于在行首或段首的，那将命中在光标前面的字符，否则将获取光标之后的字符
    /// </summary>
    public CharData? CharData
    {
        get
        {
            if (LineLayoutData.CharCount == 0)
            {
                return null;
            }
            else
            {
                return LineLayoutData.GetCharList()[HitLineCharOffset.Offset];
            }
        }
    }

    /// <summary>
    /// 获取这一行在光标之后的字符。如果光标之后没有字符或是空段，那就空
    /// </summary>
    /// <returns></returns>
    public CharData? GetCharDataInLineAfterCaretOffset()
    {
        if (LineLayoutData.CharCount == 0 || LineLayoutData.CharCount < HitOffset.Offset)
        {
            return null;
        }

        var hitCharOffset = new ParagraphCharOffset(HitOffset.Offset/* + 1 光标坐标系转换字符坐标系，这里不需要加一的*/);
        var hitCharData = ParagraphData.GetCharData(hitCharOffset);
        return hitCharData;
    }

    /// <summary>
    /// 是否一个空段
    /// </summary>
    public bool IsEmptyParagraph => ParagraphData.IsEmptyParagraph;

    internal ParagraphData ParagraphData => LineLayoutData.CurrentParagraph;
    internal ParagraphCaretOffset HitOffset { get; }

    /// <summary>
    /// 是在段落的末尾
    /// </summary>
    public bool IsParagraphEnd => HitOffset.Offset == ParagraphData.CharCount;

    /// <summary>
    /// 是否命中到行的末尾。命中到行的末尾时，所取的字符是光标前一个字符
    /// </summary>
    /// <remarks>
    /// 空段时，只设置命中到行、段首，不会设置命中到行末尾。即不会存在同时 <see cref="IsLineEnd"/> 和 <see cref="IsLineStart"/> 为 true 的情况
    /// </remarks>
    public bool IsLineEnd { get; }

    internal LineLayoutData LineLayoutData { get; }

    /// <summary>
    /// 光标偏移量
    /// </summary>
    public CaretOffset CaretOffset { get; }

    /// <summary>
    /// 获取光标的范围，坐标相对于文档左上角。可用于获取光标渲染范围。这只是一个帮助方法而已
    /// </summary>
    /// <param name="caretWidth"></param>
    /// <param name="isOvertypeMode">是否覆盖模式，覆盖模式选择返回的是后一个字符的下划线。如果处于段落末尾，则依然显示竖线的光标。如果业务开发者想要自己定制，那就不要调用此帮助方法好了</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public TextRect GetCaretBounds(double caretWidth, bool isOvertypeMode = false)
    {
        var charData = CharData;
        TextRect lineBounds = LineBounds;
        var startPoint = charData?.GetStartPoint() ?? lineBounds.LeftTop;
        TextSize textSize;
        if (charData?.Size is not null)
        {
            textSize = charData.Size.Value;
        }
        else
        {
            textSize = lineBounds.TextSize;
        }

        switch (TextEditor.ArrangingType)
        {
            case ArrangingType.Horizontal:

                if (isOvertypeMode && !IsParagraphEnd)
                {
                    // 如果是覆盖模式，应该显示后一个字符的下划线。特殊处理： 处于行末的情况，应该取下一行的字符
                    // 先继续简单处理，如果是行末，那就显示竖线
                    if (IsLineEnd)
                    {
                        // 继续往下执行，显示竖线
                    }
                    else
                    {
                        // 显示下一个字符的下划线
                        var nextCharData = GetCharDataInLineAfterCaretOffset();
                        if (nextCharData?.Size is {} size)
                        {
                            return new TextRect(nextCharData.GetStartPoint().X, lineBounds.Bottom - caretWidth, size.Width,
                                height: caretWidth);
                        }
                    }
                }

                var (x, y) = startPoint;

                // 可以获取到起始点，那肯定存在尺寸
                if (IsLineStart)
                {
                    // 如果命中到行的开始，那就是首个字符之前，不能加上字符的尺寸
                }
                else
                {
                    // 如果命中不是行的开始，那应该让光标放在字符的后面，即 x 加上字符的宽度
                    x += textSize.Width;
                }
                var width = caretWidth;
                //var height =
                //    LineSpacingCalculator.CalculateLineHeightWithLineSpacing(TextEditor,
                //        TextEditor.DocumentManager.CurrentCaretRunProperty, 1);
                //y += textSize.Height - height;
                var height = textSize.Height;

                var rectangle = new TextRect(x, y, width, height);
                return rectangle;
            case ArrangingType.Vertical:
                break;
            case ArrangingType.Mongolian:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new NotImplementedException();
    }
}
