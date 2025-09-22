using Avalonia.Input;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Utils.Patterns;

using System.Diagnostics;
using Avalonia;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    #region 鼠标

    public virtual void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.Pointer.IsPrimary)
        {
            // 多指就不要捣乱
            return;
        }

        _isMouseDown = true;

        PointerPoint currentPoint = e.GetCurrentPoint(TextEditor);
        if (currentPoint.Properties.IsRightButtonPressed)
        {
            // 右击
            // todo 右击菜单
            return;
        }
        else
        {
            _inputGesture.RecordDown(TextEditor, e);
            Point position = e.GetPosition(TextEditor);
            TextPoint clickPoint = position.ToTextPoint();

            if (_inputGesture.ClickCount % 2 == 0)
            {
                HandleDoubleClick(clickPoint);
            }
            else if (_inputGesture.ClickCount % 2 == 1)
            {
                if (e.Handled)
                {
                    // 理论上不会进入此分支
                    return;
                }

                // HandleSingleClick
                if (TextEditorCore.TryHitTest(clickPoint, out var result))
                {
                    _isHitSelection = !TextEditorCore.CurrentSelection.IsEmpty && TextEditorCore.CurrentSelection.Contains(result.HitCaretOffset);

                    if (!_isHitSelection)
                    {
                        // 没有命中到选择，那就设置当前光标
                        TextEditorCore.CurrentCaretOffset = result.HitCaretOffset;
                    }

                    // 获取焦点的同时捕获鼠标，这样既可以收到输入法，也可以用来后续拖动鼠标选中内容
                    TextEditor.Focus(NavigationMethod.Directional);
                    //Keyboard.Focus(TextEditor);
                    //FocusManager.SetFocusedElement(TextEditor, TextEditor);
                    //Mouse.Capture(TextEditor, CaptureMode.SubTree);

                    e.Handled = true;
                }
                else
                {
                    Debug.Fail("理论上一定能命中成功");
                }
            }
        }
    }

    public virtual void OnPointerMoved(PointerEventArgs e)
    {
        if (!e.Pointer.IsPrimary)
        {
            // 多指就不要捣乱
            return;
        }

        if (_isMouseDown)
        {
            if (_isHitSelection)
            {
                // todo HandleDragText(); 拖拽文本支持
            }
            else
            {
                //拖拽选择
                // HandleDragSelect
                if (_inputGesture.ClickCount % 2 == 0)
                {
                    // 双击不处理拖动
                    return;
                }

                var startOffset = TextEditorCore.CurrentSelection.StartOffset;
                var position = e.GetPosition(TextEditor);
                if (TextEditorCore.TryHitTest(position.ToTextPoint(), out var result))
                {
                    if (result.IsOutOfTextCharacterBounds)
                    {
                        // 如果拖动过程超过文本了，那应该忽略，而不是获取文档末尾的 HitCaretOffset 值
                    }
                    else
                    {
                        var endOffset = result.HitCaretOffset;
                        TextEditorCore.CurrentSelection = new Selection(startOffset, endOffset);
                    }
                }
                else
                {
                    Debug.Fail("理论上一定能命中成功");
                }
            }
        }
        else
        {
            // Hover
        }
    }

    public virtual void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!e.Pointer.IsPrimary)
        {
            // 多指就不要捣乱
            return;
        }

        if (_isMouseDown)
        {
            if (_isHitSelection)
            {
                var position = e.GetPosition(TextEditor);
                if (TextEditorCore.TryHitTest(position.ToTextPoint(), out var result))
                {
                    TextEditorCore.CurrentCaretOffset = result.HitCaretOffset;
                }
            }
        }

        _isMouseDown = false;
        //Mouse.Capture(TextEditor, CaptureMode.None);
    }

    #endregion

    #region 键盘

    public virtual void OnTextInput(TextInputEventArgs e)
    {
        if (e.Handled ||
            string.IsNullOrEmpty(e.Text) ||
            e.Text == "\x1b" ||
            // 退格键 \b 键
            e.Text == "\b" ||
            //emoji包围符
            e.Text == "\ufe0f")
            return;

        RawTextInput(e.Text);
    }

    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            Delete();
            return;
        }
        else if (e.Key == Key.Back)
        {
            Backspace();
            return;
        }
        else if (e.Key == Key.Enter)
        {
            BreakLine();
            return;
        }
        else if (e.Key == Key.Insert)
        {
            SwitchOvertypeMode();
            return;
        }

        if (TextEditorCore.IsDirty)
        {
            // 如果有明确布局的话，可以在这里加上明确布局
            TextEditor.ForceLayout();
        }

        if (e.KeyModifiers == KeyModifiers.None)
        {
            if (e.Key == Key.Up)
            {
                MoveCaret(CaretMoveType.UpByLine);
            }
            else if (e.Key == Key.Down)
            {
                MoveCaret(CaretMoveType.DownByLine);
            }
            else if (e.Key == Key.Left)
            {
                MoveCaret(CaretMoveType.LeftByCharacter);
            }
            else if (e.Key == Key.Right)
            {
                MoveCaret(CaretMoveType.RightByCharacter);
            }
        }
    }

    public virtual void OnKeyUp(KeyEventArgs e)
    {
        if (!TextEditor.IsInEditingInputMode)
        {
            // 没有进入编辑模式，不处理键盘事件
            return;
        }
    }

    #endregion

    #region 方向键

    public virtual partial void MoveCaret(CaretMoveType type)
    {
        TextEditor.TextEditorCore.MoveCaret(type);
    }

    #endregion

    #region 剪贴板


    #endregion
}