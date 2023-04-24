using LightTextEditorPlus.Core.Carets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;

namespace LightTextEditorPlus.Core;

// 此文件存放键盘光标相关逻辑
public partial class TextEditorCore
{
    /// <summary>
    /// 移动光标。如已知 <see cref="CaretOffset"/> 可直接给 <see cref="CurrentCaretOffset"/> 属性赋值
    /// </summary>
    /// <param name="type"></param>
    public void MoveCaret(CaretMoveType type)
    {
        var caretOffset = GetNewCaretOffset(type);
        CaretManager.CurrentCaretOffset = caretOffset;
    }

    /// <summary>
    /// 移动光标
    /// </summary>
    /// <param name="caretOffset"></param>
    [Obsolete("如已知 CaretOffset 可直接给 CurrentCaretOffset 属性赋值。此方法仅仅只是用来告诉你正确的方法应该是给 CurrentCaretOffset 属性赋值")]
    public void MoveCaret(CaretOffset caretOffset) => CaretManager.CurrentCaretOffset = caretOffset;

    /// <summary>
    /// 根据键盘操作获取光标导航
    /// </summary>
    /// <param name="caretMoveType"></param>
    /// <returns></returns>
    public CaretOffset GetNewCaretOffset(CaretMoveType caretMoveType)
    {
        if (IsDirty)
        {
            // 理论上不能进来，必须等待文本布局完成才能进入
            // 不使用 VerifyNotDirty 的原因是为了方便在这里打断点
            ThrowTextEditorDirtyException();
        }

        switch (caretMoveType)
        {
            case CaretMoveType.Left:
                return GetLeftCaretOffset();
            case CaretMoveType.Right:
                return GetRightCaretOffset();
            case CaretMoveType.Up:
                return GetUpCaretOffset();
            case CaretMoveType.Down:
                return GetDownCaretOffset();
            case CaretMoveType.ControlLeft:
                return GetCtrlLeftCaretOffset();
            case CaretMoveType.ControlRight:
                return GetCtrlRightCaretOffset();
            case CaretMoveType.ControlUp:
                return GetCtrlUpCaretOffset();
            case CaretMoveType.ControlDown:
                return GetCtrlDownCaretOffset();
            case CaretMoveType.LeftByCharacter:
                return GetPreviousCharacterCaretOffset();
            case CaretMoveType.RightByCharacter:
                return GetNextCharacterCaretOffset();
            case CaretMoveType.LeftByWord:
                return GetPreviousWordCaretOffset();
            case CaretMoveType.RightByWord:
                return GetNextCharacterCaretOffset();
            case CaretMoveType.UpByLine:
                return GetPreviousLineCaretOffset();
            case CaretMoveType.DownByLine:
                return GetNextLineCaretOffset();
            case CaretMoveType.LineStart:
                return GetLineStartCaretOffset();
            case CaretMoveType.LineEnd:
                return GetLineEndCaretOffset();
            case CaretMoveType.DocumentStart:
                return GetDocumentStartCaretOffset();
            case CaretMoveType.DocumentEnd:
                return GetDocumentEndCaretOffset();
            default:
                // 不知道传入啥，那就返回当前的光标即可
                return CaretManager.CurrentCaretOffset;
        }
    }

    private CaretOffset GetDocumentEndCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetDocumentStartCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetLineEndCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetLineStartCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetNextLineCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetPreviousLineCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetPreviousWordCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetNextCharacterCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetPreviousCharacterCaretOffset()
    {
        var currentSelection = CaretManager.CurrentSelection;
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
        bool atLineStart = IsAtLineStart(newOffset);
        return new CaretOffset(newOffset, atLineStart);
    }

    private CaretOffset GetCtrlDownCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetCtrlUpCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetCtrlRightCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetCtrlLeftCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetDownCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetUpCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetRightCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetLeftCaretOffset()
    {
        throw new NotImplementedException();
    }

    #region 辅助方法

    private bool IsAtLineStart(int caretOffset)
    {
        var renderInfoProvider = GetRenderInfo();
        // 先假定是行首，如果行首能够获取到首个字符，证明是行首
        CaretRenderInfo caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(new CaretOffset(caretOffset,true));
        // 如果获取到行末也是可以设置为行首，毕竟不知道情况是怎样
        return caretRenderInfo.HitLineOffset == 0 || caretRenderInfo.LineLayoutData.CharCount == caretRenderInfo.HitLineOffset;
    }

    #endregion
}
