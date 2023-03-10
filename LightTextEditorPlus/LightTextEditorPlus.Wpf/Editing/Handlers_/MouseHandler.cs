using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

internal class MouseHandler
{
    public MouseHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;

        textEditor.TextEditorCore.ArrangingTypeChanged += TextEditorCore_ArrangingTypeChanged;
    }

    private TextEditor TextEditor { get; }



    #region 光标

    /// <summary>
    /// 文本的光标样式。由于 <see cref="Cursor"/> 属性将会被此类型赋值，导致如果想要定制光标，将会被覆盖
    /// </summary>
    public CursorStyles? CursorStyles
    {
        set
        {
            _cursorStyles = value;
            RefreshCursor();
        }
        get => _cursorStyles;
    }

    private CursorStyles? _cursorStyles;

    private void RefreshCursor()
    {
        if (CursorStyles is not null)
        {
            TextEditor.Cursor = CursorStyles.Cursor;
            return;
        }

        var cursor = TextEditor.TextEditorCore.ArrangingType switch
        {
            ArrangingType.Horizontal => Cursors.IBeam,
            // todo 竖排文本的光标
            _ => Cursors.IBeam,
        };
        TextEditor.Cursor = cursor;
    }

    private void TextEditorCore_ArrangingTypeChanged(object? sender, Core.Events.TextEditorValueChangeEventArgs<ArrangingType> e)
    {
        // 布局方式变更，修改光标方向
        RefreshCursor();
    }

    #endregion
}


