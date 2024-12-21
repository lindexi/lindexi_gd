using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;

using System;
using System.Linq;

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
        if (textEditor.IsDirty)
        {
            // 理论上不能进来，必须等待文本布局完成才能进入
            // 在 VerifyNotDirty 外面套 IsDirty 判断的原因是为了方便在这里打断点
            textEditor.VerifyNotDirty();
        }

        switch (caretMoveType)
        {
            case CaretMoveType.Left:
                return GetLeftCaretOffset(textEditor);
            case CaretMoveType.Right:
                return GetRightCaretOffset(textEditor);
            case CaretMoveType.Up:
                return GetUpCaretOffset(textEditor);
            case CaretMoveType.Down:
                return GetDownCaretOffset(textEditor);
            case CaretMoveType.ControlLeft:
                return GetCtrlLeftCaretOffset(textEditor);
            case CaretMoveType.ControlRight:
                return GetCtrlRightCaretOffset(textEditor);
            case CaretMoveType.ControlUp:
                return GetCtrlUpCaretOffset(textEditor);
            case CaretMoveType.ControlDown:
                return GetCtrlDownCaretOffset(textEditor);
            case CaretMoveType.LeftByCharacter:
                return GetPreviousCharacterCaretOffset(textEditor);
            case CaretMoveType.RightByCharacter:
                return GetNextCharacterCaretOffset(textEditor);
            case CaretMoveType.LeftByWord:
                return GetPreviousWordCaretOffset(textEditor);
            case CaretMoveType.RightByWord:
                return GetNextCharacterCaretOffset(textEditor);
            case CaretMoveType.UpByLine:
                return GetPreviousLineCaretOffset(textEditor);
            case CaretMoveType.DownByLine:
                return GetNextLineCaretOffset(textEditor);
            case CaretMoveType.LineStart:
                return GetLineStartCaretOffset(textEditor);
            case CaretMoveType.LineEnd:
                return GetLineEndCaretOffset(textEditor);
            case CaretMoveType.DocumentStart:
                return GetDocumentStartCaretOffset(textEditor);
            case CaretMoveType.DocumentEnd:
                return GetDocumentEndCaretOffset(textEditor);
            default:
                // 不知道传入啥，那就返回当前的光标即可
                return textEditor.CaretManager.CurrentCaretOffset;
        }
    }

    private static CaretOffset GetDocumentEndCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetDocumentStartCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetLineEndCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetLineStartCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 方向键：下
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetNextLineCaretOffset(TextEditorCore textEditorCore)
    {
        // 先获取当前光标是在这一行的哪里，接着将其对应到下一行
        var currentCaretOffset = textEditorCore.CaretManager.CurrentCaretOffset;
        var renderInfoProvider = textEditorCore.GetRenderInfo();
        var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(currentCaretOffset);

        var paragraphManager = textEditorCore.DocumentManager.ParagraphManager;

        if (caretRenderInfo.LineIndex < caretRenderInfo.ParagraphData.LineLayoutDataList.Count - 1)
        {
            // 判断是否在段落内存在下一行
            var targetLine = caretRenderInfo.ParagraphData.LineLayoutDataList[caretRenderInfo.LineIndex + 1];

            // 这里拿到的是行坐标系，需要将其换算为文档光标坐标系
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
    private static CaretOffset GetPreviousLineCaretOffset(TextEditorCore textEditorCore)
    {
        // 先获取当前光标是在这一行的哪里，接着将其对应到上一行
        var currentCaretOffset = textEditorCore.CaretManager.CurrentCaretOffset;
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

    private static CaretOffset GetPreviousWordCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 方向键：右
    /// </summary>
    /// <param name="textEditorCore"></param>
    /// <returns></returns>
    private static CaretOffset GetNextCharacterCaretOffset(TextEditorCore textEditorCore)
    {
        //如果有选择，则直接返回选择的BehindOffset
        var currentSelection = textEditorCore.CaretManager.CurrentSelection;
        //如果当前选择不为空，则直接返回选择的FrontOffset
        if (!currentSelection.IsEmpty)
        {
            return currentSelection.BehindOffset;
        }
        CaretOffset currentCaretOffset = currentSelection.FrontOffset;

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
    private static CaretOffset GetPreviousCharacterCaretOffset(TextEditorCore textEditorCore)
    {
        var currentSelection = textEditorCore.CaretManager.CurrentSelection;
        //如果当前选择不为空，则直接返回选择的FrontOffset
        if (!currentSelection.IsEmpty)
        {
            return currentSelection.FrontOffset;
        }

        CaretOffset currentCaretOffset = currentSelection.FrontOffset;
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

    private static CaretOffset GetCtrlRightCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetCtrlLeftCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetDownCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetUpCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetRightCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    private static CaretOffset GetLeftCaretOffset(TextEditorCore textEditorCore)
    {
        throw new NotImplementedException();
    }

    #region 辅助方法

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

    #endregion
}
