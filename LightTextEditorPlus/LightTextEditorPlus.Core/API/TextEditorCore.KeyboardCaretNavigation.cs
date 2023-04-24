using LightTextEditorPlus.Core.Carets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                return GetPrevCharacterCaretOffset();
            case CaretMoveType.RightByCharacter:
                return GetNextCharacterCaretOffset();
            case CaretMoveType.LeftByWord:
                return GetPrevWordCaretOffset();
            case CaretMoveType.RightByWord:
                return GetNextCharacterCaretOffset();
            case CaretMoveType.UpByLine:
                return GetPrevLineCaretOffset();
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

    private CaretOffset GetPrevLineCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetPrevWordCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetNextCharacterCaretOffset()
    {
        throw new NotImplementedException();
    }

    private CaretOffset GetPrevCharacterCaretOffset()
    {
        throw new NotImplementedException();
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
}
