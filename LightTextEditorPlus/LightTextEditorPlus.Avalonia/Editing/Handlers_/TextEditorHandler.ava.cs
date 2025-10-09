using System;
using Avalonia.Input;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Utils.Patterns;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{

    #region 鼠标

    /// <summary>
    /// 指针按下
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnPointerPressed(PointerPressedEventArgs e)
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

                if (HandleSingleClick(clickPoint))
                {
                    // 获取焦点的同时捕获鼠标，这样既可以收到输入法，也可以用来后续拖动鼠标选中内容
                    TextEditor.Focus(NavigationMethod.Directional);
                    //Keyboard.Focus(TextEditor);
                    //FocusManager.SetFocusedElement(TextEditor, TextEditor);
                    //Mouse.Capture(TextEditor, CaptureMode.SubTree);

                    e.Handled = true;
                }
            }
        }
    }

    /// <summary>
    /// 指针移动
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnPointerMoved(PointerEventArgs e)
    {
        if (!e.Pointer.IsPrimary)
        {
            // 多指就不要捣乱
            return;
        }

        if (_isMouseDown)
        {
            var position = e.GetPosition(TextEditor);
            TextPoint textPoint = position.ToTextPoint();

            if (_isHitSelection)
            {
                HandleDragText(in textPoint);
            }
            else
            {
                //拖拽选择
                HandleDragSelect(in textPoint);
            }
        }
        else
        {
            // Hover
        }
    }

    /// <summary>
    /// 指针抬起
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnPointerReleased(PointerReleasedEventArgs e)
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

    /// <summary>
    /// 当文本输入
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnTextInput(TextInputEventArgs e)
    {
        if (e.Handled ||
            string.IsNullOrEmpty(e.Text) ||
            e.Text == "\e" ||
            // 退格键 \b 键
            e.Text == "\b" ||
            //emoji包围符
            e.Text == "\ufe0f")
            return;

        RawTextInput(e.Text);
    }

    /// <summary>
    /// 键盘按下
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnKeyDown(KeyEventArgs e)
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

        InputEnsureLayout();

        if (e.KeyModifiers == KeyModifiers.None)
        {
            if (e.Key == Key.Up)
            {
                MoveCaret(CaretMoveType.UpByLine);
                return;
            }
            else if (e.Key == Key.Down)
            {
                MoveCaret(CaretMoveType.DownByLine);
                return;
            }
            else if (e.Key == Key.Left)
            {
                MoveCaret(CaretMoveType.LeftByCharacter);
                return;
            }
            else if (e.Key == Key.Right)
            {
                MoveCaret(CaretMoveType.RightByCharacter);
                return;
            }
            else if (e.Key == Key.Home)
            {
                MoveCaret(CaretMoveType.LineStart);
                return;
            }
            else if (e.Key == Key.End)
            {
                MoveCaret(CaretMoveType.LineEnd);
                return;
            }
        }
    }

    /// <summary>
    /// 键盘抬起
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnKeyUp(KeyEventArgs e)
    {
        if (!TextEditor.IsInEditingInputMode)
        {
            // 没有进入编辑模式，不处理键盘事件
            return;
        }
    }

    internal void InputEnsureLayout()
    {
        if (TextEditorCore.IsDirty)
        {
            // 如果有明确布局的话，可以在这里加上明确布局
            TextEditor.ForceLayout();
        }
    }

    #region 快捷键

    ///// <summary>
    ///// 尝试执行快捷键
    ///// </summary>
    ///// <param name="e"></param>
    ///// <returns></returns>
    //protected bool TryExecuteShortCut(KeyEventArgs e)
    //{
    //    TextEditor.KeyBindings.Add(new KeyBinding()
    //    {
            
    //    });

        

    //    ShortCutManager.FindCommandBinding(new TextEditorKeyGesture(e.Key, e.KeyModifiers))
    //}

    #endregion 快捷键

    #endregion 键盘

    #region 方向键

    /// <summary>
    /// 移动光标方向
    /// </summary>
    /// <param name="type"></param>
    protected internal virtual partial void MoveCaret(CaretMoveType type)
    {
        TextEditor.TextEditorCore.MoveCaret(type);
    }

    #endregion 方向键

    #region 剪贴板

    /// <summary>
    /// 当拷贝时
    /// </summary>
    protected internal virtual void OnCopy()
    {
        if (TextEditor.CurrentSelection.IsEmpty)
        {
            return;
        }

        string text = TextEditor.GetSelectedText();
         _ = GetClipboard()?.SetTextAsync(text);
    }

    /// <summary>
    /// 当剪切时
    /// </summary>
    protected internal virtual void OnCut()
    {
        Selection currentSelection = TextEditor.CurrentSelection;
        if (currentSelection.IsEmpty)
        {
            return;
        }

        string text = TextEditor.GetText(in currentSelection);
         _ = GetClipboard()?.SetTextAsync(text);
        TextEditor.Remove(in currentSelection);
    }

    /// <summary>
    /// 当粘贴时
    /// </summary>
    protected internal virtual void OnPaste()
    {
        if (GetClipboard() is not { } clipboard)
        {
            return;
        }

        // 切换异步调用的同时规避 async void 不安全写法
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            string? text = await clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(text))
            {
                OnPastePlainText(text);
            }
        });
    }

    private IClipboard? GetClipboard()
    {
        if (TopLevel.GetTopLevel(TextEditor) is {} topLevel)
        {
            return topLevel.Clipboard;
        }

        return null;
    }

    #endregion 剪贴板
}
