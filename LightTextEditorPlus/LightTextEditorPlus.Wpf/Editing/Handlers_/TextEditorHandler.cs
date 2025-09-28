using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    #region 鼠标相关

    /// <summary>
    /// 鼠标按下
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnMouseDown(MouseButtonEventArgs e)
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
            var position = e.GetPosition(TextEditor);
            TextPoint textPoint = position.ToTextPoint();

            if (_inputGesture.ClickCount % 2 == 0)
            {
                HandleDoubleClick(in textPoint);
            }
            else if (_inputGesture.ClickCount % 2 == 1)
            {
                if (e.Handled)
                {
                    // 理论上不会进入此分支
                    return;
                }

                if (HandleSingleClick(in textPoint))
                {
                    // 获取焦点的同时捕获鼠标，这样既可以收到输入法，也可以用来后续拖动鼠标选中内容
                    TextEditor.Focus();
                    //Keyboard.Focus(TextEditor);
                    //FocusManager.SetFocusedElement(TextEditor, TextEditor);

                    Mouse.Capture(TextEditor, CaptureMode.SubTree);
                    e.Handled = true;
                }
            }
        }
    }

    /// <summary>
    /// 鼠标移动
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnMouseMove(MouseEventArgs e)
    {
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
    /// 鼠标抬起
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnMouseUp(MouseButtonEventArgs e)
    {
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
        Mouse.Capture(TextEditor, CaptureMode.None);
    }

    /// <summary>
    /// 鼠标进入
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnMouseEnter(MouseEventArgs e)
    {
        //Debug.WriteLine("TextEditor_MouseEnter");
        UpdateCursorView();
    }

    /// <summary>
    /// 失去焦点
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnLostMouseCapture(MouseEventArgs e)
    {
        //Debug.WriteLine("TextEditor_LostMouseCapture");
        if (_isMouseDown)
        {
            // 被失焦了
            _isMouseDown = false;
            Mouse.Capture(TextEditor, CaptureMode.None);
        }
    }

    #region 光标

    private Cursor GetVerticalCursor()
    {
        var verticalCursor = TextEditor.CursorStyles?.VerticalCursor;
        if (verticalCursor is not null)
        {
            return verticalCursor;
        }

        const string url = "pack://application:,,,/LightTextEditorPlus.Wpf;component/Resources/Cursors/";
        const string verticalTextUrl = url + "VerticalText.cur";
        verticalCursor = new Cursor(Application
            .GetResourceStream(new Uri(
                verticalTextUrl,
                UriKind.RelativeOrAbsolute))!.Stream);
        return verticalCursor;
    }

    /// <summary>
    /// 更新光标视觉显示样式内容
    /// </summary>
    protected internal virtual void UpdateCursorView()
    {
        if (TextEditor.CursorStyles is not null)
        {
            TextEditor.Cursor = TextEditor.CursorStyles.Cursor;
            return;
        }

        Cursor? cursor;
        if (TextEditor.TextEditorCore.ArrangingType == ArrangingType.Horizontal)
        {
            cursor = Cursors.IBeam;
        }
        else if (TextEditor.TextEditorCore.ArrangingType.IsVertical)
        {
            cursor = GetVerticalCursor();
        }
        else
        {
            cursor = Cursors.IBeam;
        }

        TextEditor.Cursor = cursor;
    }

    #endregion

    #endregion

    /// <summary>
    /// 键盘按下事件
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BreakLine();
            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// 框架内触发的文本输入事件
    /// </summary>
    /// <param name="e"></param>
    protected internal virtual void OnTextInput(TextCompositionEventArgs e)
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

    #region 方向键

    protected internal virtual partial void MoveCaret(CaretMoveType type)
    {
        type = TransformCaretMove(type);
        TextEditor.TextEditorCore.MoveCaret(type);
    }

    /// <summary>
    /// 根据文本框实际的视觉上的旋转角度，优化键盘方向。让键盘方向控制的光标符合正视觉方向
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private CaretMoveType TransformCaretMove(CaretMoveType type)
    {
        CaretMoveType returnValue = (CaretMoveType) CaretTransformDirectionHelper.TransformDirection((int) type, TextEditor);

        return returnValue;
    }

    #endregion

    #region 剪贴板

    /// <summary>
    /// 当拷贝时
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected internal virtual void OnCopy(object sender, ExecutedRoutedEventArgs e)
    {
        if (TextEditor.CurrentSelection.IsEmpty)
        {
            return;
        }

        string text = TextEditor.GetSelectedText();
        Clipboard.SetText(text);
    }

    /// <summary>
    /// 当剪切时
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected internal virtual void OnCut(object sender, ExecutedRoutedEventArgs e)
    {
        Selection currentSelection = TextEditor.CurrentSelection;
        if (currentSelection.IsEmpty)
        {
            return;
        }

        string text = TextEditor.GetText(in currentSelection);
        Clipboard.SetText(text);
        TextEditor.Remove(in currentSelection);
    }

    /// <summary>
    /// 当粘贴时
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected internal virtual void OnPaste(object sender, ExecutedRoutedEventArgs e)
    {
        IDataObject? dataObject = Clipboard.GetDataObject();
        if (dataObject is null)
        {
            return;
        }

        if (dataObject.GetData(DataFormats.Text) is string)
        {
            // 不能用 dataObject.GetData(DataFormats.Text) 的结果，因为会出现中文变问号的情况
            var text = Clipboard.GetText();
            OnPastePlainText(text);
        }
    }

    #endregion
}
