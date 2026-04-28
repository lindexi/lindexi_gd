using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core.Carets;

internal static class KeyboardCaretNavigationHelper
{
    /// <summary>
    /// 根据键盘操作获取光标导航
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="caretMoveType"></param>
    /// <returns></returns>
    public static CaretOffset GetNewCaretOffset(TextEditorCore textEditor, CaretMoveType caretMoveType)
    {
        return GetNewCaretOffset(textEditor, caretMoveType, textEditor.CaretManager.CurrentCaretOffset,
            ignoreCurrentSelection: false);
    }

    public static CaretOffset GetNewCaretOffset(TextEditorCore textEditor, CaretMoveType caretMoveType,
        CaretOffset currentCaretOffset, bool ignoreCurrentSelection)
    {
        if (textEditor.IsDirty)
        {
            // 理论上不能进来，必须等待文本布局完成才能进入
            // 在 VerifyNotDirty 外面套 IsDirty 判断的原因是为了方便在这里打断点
            textEditor.VerifyNotDirty();
        }

        switch (caretMoveType)
        {
            case CaretMoveType.Left:
                return GetLeftCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.Right:
                return GetRightCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.Up:
                return GetUpCaretOffset(textEditor, currentCaretOffset);
            case CaretMoveType.Down:
                return GetDownCaretOffset(textEditor, currentCaretOffset);
            case CaretMoveType.ControlLeft:
                return GetCtrlLeftCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.ControlRight:
                return GetCtrlRightCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.ControlUp:
                return GetCtrlUpCaretOffset(textEditor);
            case CaretMoveType.ControlDown:
                return GetCtrlDownCaretOffset(textEditor);
            case CaretMoveType.LeftByCharacter:
                return GetPreviousCharacterCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.RightByCharacter:
                return GetNextCharacterCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.LeftByWord:
                return GetPreviousWordCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.RightByWord:
                return GetNextWordCaretOffset(textEditor, currentCaretOffset, ignoreCurrentSelection);
            case CaretMoveType.UpByLine:
                return GetPreviousLineCaretOffset(textEditor, currentCaretOffset);
            case CaretMoveType.DownByLine:
                return GetNextLineCaretOffset(textEditor, currentCaretOffset);
            case CaretMoveType.LineStart:
                return GetLineStartCaretOffset(textEditor, currentCaretOffset);
            case CaretMoveType.LineEnd:
                return GetLineEndCaretOffset(textEditor, currentCaretOffset);
            case CaretMoveType.DocumentStart:
                return GetDocumentStartCaretOffset(textEditor);
            case CaretMoveType.DocumentEnd:
                return GetDocumentEndCaretOffset(textEditor);
            default:
                return currentCaretOffset;
        }
    }

    private static CaretOffset GetDocumentEndCaretOffset(TextEditorCore textEditorCore)
    {
        return textEditorCore.DocumentManager.GetDocumentEndCaretOffset();
    }

    private static CaretOffset GetDocumentStartCaretOffset(TextEditorCore textEditorCore)
    {
        return textEditorCore.DocumentManager.GetDocumentStartCaretOffset();
    }

    /// <summary>
    /// 方向键：Home 获取行首的光标位置
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetLineStartCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset)
    {
        var renderInfoProvider = textEditorCore.GetRenderInfo();
        var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(currentCaretOffset);

        CaretOffset caretOffset = caretRenderInfo.LineLayoutData.ToCaretOffset(new LineCaretOffset(0));
        return caretOffset;
    }

    /// <summary>
    /// 方向键：End 获取行末的光标位置
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetLineEndCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset)
    {
        var renderInfoProvider = textEditorCore.GetRenderInfo();
        var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(currentCaretOffset);

        LineLayoutData lineLayoutData = caretRenderInfo.LineLayoutData;
        CaretOffset caretOffset = lineLayoutData.ToCaretOffset(new LineCaretOffset(lineLayoutData.CharCount));
        return caretOffset;
    }

    /// <summary>
    /// 方向键：下
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetNextLineCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset)
    {
        // 先获取当前光标是在这一行的哪里，接着将其对应到下一行
        var renderInfoProvider = textEditorCore.GetRenderInfo();
        var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(currentCaretOffset);

        var paragraphManager = textEditorCore.DocumentManager.ParagraphManager;

        if (caretRenderInfo.LineIndex < caretRenderInfo.ParagraphData.LineLayoutDataList.Count - 1)
        {
            // 判断是否在段落内存在下一行
            var targetLine = caretRenderInfo.ParagraphData.LineLayoutDataList[caretRenderInfo.LineIndex + 1];

            // 为什么不能取 caretRenderInfo 的 CaretOffset 属性？因为这里需要计算下一行 targetLine 的相对文档光标坐标系。即 caretRenderInfo.LineLayoutData.Index + 1 == targetLine.Index
            //CaretOffset caretOffset = caretRenderInfo.CaretOffset;

            // 这里拿到的是行坐标系，需要将其换算为文档光标坐标系
            // 转换过程中，自动适配行内的字符数量。如有以下两行
            // 123123|123
            // abc
            // 此时按 下 键，光标之会在第二行的 abc 之后，不会出现越界问题
            var documentCaretOffset = targetLine.ToCaretOffset(caretRenderInfo.HitLineCaretOffset);
            return new CaretOffset(documentCaretOffset.Offset, currentCaretOffset.IsAtLineStart);
        }
        else if (caretRenderInfo.ParagraphIndex < paragraphManager.GetParagraphList().Count - 1)
        {
            // 取下一段的首行
            ParagraphData paragraphData = textEditorCore.DocumentManager.ParagraphManager.GetParagraph(caretRenderInfo.ParagraphIndex + 1);
            var targetLine = paragraphData.LineLayoutDataList[0];

            // 这里拿到的是行坐标系，需要将其换算为文档光标坐标系
            var documentCaretOffset = targetLine.ToCaretOffset(caretRenderInfo.HitLineCaretOffset);
            return new CaretOffset(documentCaretOffset.Offset, currentCaretOffset.IsAtLineStart);
        }

        // 如果是最后一段的最后一行，那就啥都不更新
        return currentCaretOffset;
    }

    /// <summary>
    /// 方向键：上
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    /// 上下行也许后续需要考虑通过命中测试，因为可能不同的行的文本的字符宽度不相同，例如以下情况
    /// 123一123
    /// 1231|23
    /// 以上光标向上，需要考虑放在字符 一 的左右
    private static CaretOffset GetPreviousLineCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset)
    {
        // 先获取当前光标是在这一行的哪里，接着将其对应到上一行
        var renderInfoProvider = textEditorCore.GetRenderInfo();
        var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(currentCaretOffset);

        // 如果是首段首行，那就啥都不更新
        if (caretRenderInfo.LineIndex == 0 && caretRenderInfo.ParagraphIndex == 0)
        {
            return currentCaretOffset;
        }

        if (caretRenderInfo.LineIndex > 0)
        {
            // 这是段落里面的一行，且存在上一行
            var targetLine = caretRenderInfo.ParagraphData.LineLayoutDataList[caretRenderInfo.LineIndex - 1];

            // 这里拿到的是行坐标系，需要将其换算为文档光标坐标系
            var documentCaretOffset = targetLine.ToCaretOffset(caretRenderInfo.HitLineCaretOffset);
            return new CaretOffset(documentCaretOffset.Offset, currentCaretOffset.IsAtLineStart);
        }
        else
        {
            // 需要取上一段的最后一行
            ParagraphData paragraphData = textEditorCore.DocumentManager.ParagraphManager.GetParagraph(caretRenderInfo.ParagraphIndex - 1);
            var lastLine = paragraphData.LineLayoutDataList.Last();
            var targetLine = lastLine;

            // 不能超过行的文本数量（在targetLine.ToCaretOffset里处理的逻辑）
            // 什么情况会发生超过行的文本数量？如以下情况
            // 123123123
            // 123
            // 123123|
            // 此时的光标向上，将会进入首段的末行，这一行的数量是不够末段首行的长度的

            // 这里拿到的是行落坐标系，需要将其换算为文档光标坐标系
            var documentCaretOffset = targetLine.ToCaretOffset(caretRenderInfo.HitLineCaretOffset);
            return new CaretOffset(documentCaretOffset.Offset, currentCaretOffset.IsAtLineStart);
        }
    }

    /// <summary>
    /// 向左一个单词
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static CaretOffset GetPreviousWordCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        var currentSelection = GetCurrentSelection(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
        if (!currentSelection.IsEmpty)
        {
            return currentSelection.FrontOffset;
        }

        currentCaretOffset = currentSelection.FrontOffset;
        if (currentCaretOffset.Offset == 0)
        {
            // 文档开头，不能再往前跳了
            return currentCaretOffset;
        }

        var wordSelection = GetCaretWord(currentCaretOffset, textEditorCore);
        if (wordSelection.Contains(in currentCaretOffset)
            // 不能在单词开头，如果在单词开头，则需要跳到前一个单词
            && wordSelection.FrontOffset.Offset != currentCaretOffset.Offset)
        {
            // 如果命中到了在单词内，则尝试跳到单词前面
            return ToResult(textEditorCore, wordSelection.FrontOffset);
        }

        // 判断是否在段落的首部。如果是，则仅做段落跳跃
        ITextParagraph textParagraph = textEditorCore.DocumentManager.GetParagraph(in currentCaretOffset);
        DocumentOffset paragraphStartOffset = textParagraph.GetParagraphStartOffset();
        var currentParagraphCharCaret = currentCaretOffset.Offset - paragraphStartOffset.Offset;
        if (currentParagraphCharCaret == 0)
        {
            // 已经在段首了，不能再往前跳了。直接去到上一段的末尾好了
            return ToResult(textEditorCore, new CaretOffset(currentCaretOffset.Offset - 1));
        }

        var currentCharIndex = currentParagraphCharCaret;
        TextReadOnlyListSpan<CharData> charDataList = textParagraph.GetParagraphCharDataList();
        if (GetCaretWordHelper.IsPunctuation(charDataList[currentCharIndex]))
        {
            return ToResult(textEditorCore, new CaretOffset(currentCaretOffset.Offset - 1));
        }
        else
        {
            // 不知道什么情况
            return GetPreviousCharacterCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection: true);
        }
    }

    /// <summary>
    /// 向右一个单词
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetNextWordCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        var currentSelection = GetCurrentSelection(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
        if (!currentSelection.IsEmpty)
        {
            return currentSelection.BehindOffset;
        }

        // 这里 BehindOffset 和 FrontOffset 是一样的。只是取 BehindOffset 更符合逻辑
        currentCaretOffset = currentSelection.BehindOffset;

        if (currentCaretOffset.Offset >= textEditorCore.DocumentManager.CharCount)
        {
            // 文档末尾，不能再往后跳了
            return currentCaretOffset;
        }

        var wordSelection = GetCaretWord(currentCaretOffset, textEditorCore);
        if (wordSelection.Contains(in currentCaretOffset)
            // 不能在单词末尾，如果在单词末尾，则需要跳到下一个单词
            && wordSelection.BehindOffset.Offset != currentCaretOffset.Offset)
        {
            // 如果命中到了在单词内，则尝试跳到单词末尾
            return ToResult(textEditorCore, wordSelection.BehindOffset);
        }

        // 判断是否在段落的末尾。如果是，则仅做段落跳跃
        ITextParagraph textParagraph = textEditorCore.DocumentManager.GetParagraph(in currentCaretOffset);
        DocumentOffset paragraphStartOffset = textParagraph.GetParagraphStartOffset();
        var currentParagraphCharCaret = currentCaretOffset.Offset - paragraphStartOffset.Offset;
        if (currentParagraphCharCaret >= textParagraph.CharCount)
        {
            Debug.Assert(currentParagraphCharCaret == textParagraph.CharCount, "不可能超过当前段落的字符数量");
            // 已经在段末了，不能再往后跳了。直接去到下一段的开头好了
            return GetNextCharacterCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection: true);
        }

        // 取出这一段的字符列表
        var currentCharIndex = currentParagraphCharCaret;
        TextReadOnlyListSpan<CharData> charDataList = textParagraph.GetParagraphCharDataList();

        if (GetCaretWordHelper.IsPunctuation(charDataList[currentCharIndex]))
        {
            // 如果当前是标点符号，则直接跳过这个符号
            return ToResult(textEditorCore, new CaretOffset(currentCaretOffset.Offset + 1));
        }
        else
        {
            // 不知道什么情况。理论上不应该遇到下一个单词才对
            return GetPreviousCharacterCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection: true);
        }
    }

    /// <summary>
    /// 方向键：右
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetNextCharacterCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        //如果有选择，则直接返回选择的BehindOffset
        var currentSelection = GetCurrentSelection(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
        //如果当前选择不为空，则直接返回选择的FrontOffset
        if (!currentSelection.IsEmpty)
        {
            return currentSelection.BehindOffset;
        }
        currentCaretOffset = currentSelection.FrontOffset;

        var newOffset = currentCaretOffset.Offset + 1;
        if (newOffset > textEditorCore.DocumentManager.CharCount)
        {
            // 超过数量了，那就设置为文档数量
            return new CaretOffset(textEditorCore.DocumentManager.CharCount);
        }

        bool atLineStart = IsAtLineStart(textEditorCore, newOffset);
        return new CaretOffset(newOffset, atLineStart);
    }

    /// <summary>
    /// 方向键：左
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetPreviousCharacterCaretOffset(TextEditorCore textEditorCore,
        CaretOffset currentCaretOffset, bool ignoreCurrentSelection)
    {
        var currentSelection = GetCurrentSelection(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
        //如果当前选择不为空，则直接返回选择的FrontOffset
        if (!currentSelection.IsEmpty)
        {
            return currentSelection.FrontOffset;
        }

        currentCaretOffset = currentSelection.FrontOffset;
        if (currentCaretOffset.Offset == 0)
        {
            // 特殊值，文档开头，不能继续往前
            return currentCaretOffset;
        }
        else if (currentCaretOffset.Offset == 1)
        {
            // 特殊值，只能去到文档开头
            return new CaretOffset(0);
        }

        var newOffset = currentCaretOffset.Offset - 1;
        if (currentCaretOffset.IsAtLineStart)
        {
            // 如果当前已经是行首了，那上一个光标一定不是行首，但可能是段首
            return new CaretOffset(newOffset);
        }

        //判断是否为行首，在段内向上一个字符移动时，如果在行首，则要添加标记
        bool atLineStart = IsAtLineStart(textEditorCore, newOffset);
        return new CaretOffset(newOffset, atLineStart);
    }

    private static CaretOffset GetCtrlDownCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetCtrlUpCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetCtrlRightCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        return GetNextWordCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
    }

    private static CaretOffset GetCtrlLeftCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        return GetPreviousWordCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
    }

    private static CaretOffset GetDownCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset)
    {
        return GetNextLineCaretOffset(textEditorCore, currentCaretOffset);
    }

    private static CaretOffset GetUpCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset)
    {
        return GetPreviousLineCaretOffset(textEditorCore, currentCaretOffset);
    }

    private static CaretOffset GetRightCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        return GetNextCharacterCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
    }

    private static CaretOffset GetLeftCaretOffset(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        return GetPreviousCharacterCaretOffset(textEditorCore, currentCaretOffset, ignoreCurrentSelection);
    }

    #region 辅助方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CaretOffset ToResult(TextEditorCore textEditor, in CaretOffset newOffset)
    {
        bool atLineStart = IsAtLineStart(textEditor, newOffset.Offset);
        return new CaretOffset(newOffset.Offset, atLineStart);
    }

    private static bool IsAtLineStart(TextEditorCore textEditor, int caretOffset)
    {
        var renderInfoProvider = textEditor.GetRenderInfo();
        // 先假定是行首，如果行首能够获取到首个字符，证明是行首
        CaretRenderInfo caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(new CaretOffset(caretOffset, true), isTestingLineStart: true);
        if (caretRenderInfo.HitLineCaretOffset.Offset == 0)
        {
            return true;
        }
        // 如果获取到行末，那就需要判断是否存在下一行，如果处于段末了，那就不能指定为行首
        if (caretRenderInfo.LineLayoutData.CharCount == caretRenderInfo.HitLineCaretOffset.Offset)
        {
            if (caretRenderInfo.LineIndex < caretRenderInfo.ParagraphData.LineLayoutDataList.Count - 1)
            {
                // 证明还有下一行
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Selection GetCurrentSelection(TextEditorCore textEditorCore, CaretOffset currentCaretOffset,
        bool ignoreCurrentSelection)
    {
        if (ignoreCurrentSelection)
        {
            return currentCaretOffset.ToSelection();
        }

        return textEditorCore.CaretManager.CurrentSelection;
    }

    /// <summary>
    /// 获取传入光标所在的单词选择范围
    /// </summary>
    /// <returns></returns>
    private static Selection GetCaretWord(in CaretOffset caretOffset, TextEditorCore textEditorCore)
    {
        IWordDivider wordDivider = textEditorCore.PlatformProvider.GetWordDivider();
        var result = wordDivider.GetCaretWord(new GetCaretWordArgument(caretOffset, textEditorCore));
        return result.WordSelection;
    }

    #endregion
}
