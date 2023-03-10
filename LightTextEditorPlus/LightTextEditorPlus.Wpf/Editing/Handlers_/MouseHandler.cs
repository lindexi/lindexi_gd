using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Rendering;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LightTextEditorPlus.Utils;
using Point = System.Windows.Point;

namespace LightTextEditorPlus.Editing;

internal class MouseHandler
{
    public MouseHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;

        textEditor.TextEditorCore.ArrangingTypeChanged += (_, _) => UpdateCursor();

        textEditor.MouseDown += TextEditor_MouseDown;
        textEditor.MouseMove += TextEditor_MouseMove;
        textEditor.MouseUp += TextEditor_MouseUp;
        textEditor.MouseEnter += TextEditor_MouseEnter;
        textEditor.LostMouseCapture += TextEditor_LostMouseCapture;
    }

    private bool _isMouseDown;

    /// <summary>
    /// 是不是点到选择范围
    /// </summary>
    private bool _isHitSelection;

    private void TextEditor_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = true;
        if (e.RightButton == MouseButtonState.Pressed)
        {
            // 右击
            // todo 右击菜单
        }
        else
        {
            _inputGesture.RecordDown(TextEditor, e);

            if (_inputGesture.ClickCount % 2 == 0)
            {
                // HandleDoubleClick
                // 默认行为是双击全选，你想选词？那就不好玩了哦
                TextEditor.TextEditorCore.SelectAll();
            }
            else if (_inputGesture.ClickCount % 2 == 1)
            {
                if (e.Handled)
                {
                    // 理论上不会进入此分支
                    return;
                }

                // HandleSingleClick
                var position = e.GetPosition(TextEditor);
                TextEditor.TextEditorPlatformProvider.EnsureLayoutUpdated();
                if (TextEditorCore.TryHitTest(position.ToPoint(), out var result))
                {
                    _isHitSelection = TextEditorCore.CurrentSelection.Contains(result.HitCaretOffset);

                    if (!_isHitSelection)
                    {
                        // 没有命中到选择，那就设置当前光标
                        TextEditorCore.CurrentCaretOffset = result.HitCaretOffset;
                    }

                    // 获取焦点的同时捕获鼠标，这样既可以收到输入法，也可以用来后续拖动鼠标选中内容
                    TextEditor.Focus();
                    Mouse.Capture(TextEditor, CaptureMode.SubTree);

                    e.Handled = true;
                }
                else
                {
                    Debug.Fail("理论上一定能命中成功");
                }
            }
        }
    }

    private void TextEditor_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isMouseDown)
        {

        }
        else
        {
            // Hover
        }
    }

    private void TextEditor_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = false;
        Mouse.Capture(TextEditor, CaptureMode.None);
    }

    private void TextEditor_MouseEnter(object sender, MouseEventArgs e)
    {
        Debug.WriteLine("TextEditor_MouseEnter");
        UpdateCursor();
    }

    private void TextEditor_LostMouseCapture(object sender, MouseEventArgs e)
    {
        Debug.WriteLine("TextEditor_LostMouseCapture");
        if (_isMouseDown)
        {
            // 被失焦了
        }
    }

    private TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;


    #region 光标

    /// <summary>
    /// 文本的光标样式。由于 <see cref="Cursor"/> 属性将会被此类型赋值，导致如果想要定制光标，将会被覆盖
    /// </summary>
    public CursorStyles? CursorStyles
    {
        set
        {
            _cursorStyles = value;
            UpdateCursor();
        }
        get => _cursorStyles;
    }

    private CursorStyles? _cursorStyles;

    private void UpdateCursor()
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

    #endregion

    #region InputGestureInfo

    private readonly InputGestureInfo _inputGesture = new InputGestureInfo();

    #endregion
}