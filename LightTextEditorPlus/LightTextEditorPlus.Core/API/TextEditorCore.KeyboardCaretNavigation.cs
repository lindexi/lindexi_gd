using LightTextEditorPlus.Core.Carets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Document.Segments;

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
        return KeyboardCaretNavigationHelper.GetNewCaretOffset(this, caretMoveType);
    }
}
