using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

public partial class TextEditorHandler
{
    #region 鼠标相关

    private bool _isMouseDown;

    public virtual void OnMouseDown(MouseButtonEventArgs e)
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
                    e.Handled = true;
                }
            }
        }
    }

    public virtual void OnMouseMove(MouseEventArgs e)
    {
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

    public virtual void OnMouseUp(MouseButtonEventArgs e)
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
    /// 是不是点到选择范围
    /// </summary>
    private bool _isHitSelection;

    #region InputGestureInfo

    private readonly InputGestureInfo _inputGesture = new InputGestureInfo();

    #endregion

    /// <summary>
    /// 处理单击事件
    /// </summary>
    /// <param name="clickPoint"></param>
    /// <returns></returns>
    public virtual bool HandleSingleClick(in TextPoint clickPoint)
    {
        TextEditor
            .TextEditorPlatformProvider
            .EnsureLayoutUpdated();
        if (TextEditorCore.TryHitTest(in clickPoint, out var result))
        {
            _isHitSelection = !TextEditorCore.CurrentSelection.IsEmpty && TextEditorCore.CurrentSelection.Contains(result.HitCaretOffset);

            if (!_isHitSelection)
            {
                // 没有命中到选择，那就设置当前光标
                TextEditorCore.CurrentCaretOffset = result.HitCaretOffset;
            }

            // 获取焦点的同时捕获鼠标，这样既可以收到输入法，也可以用来后续拖动鼠标选中内容
            TextEditor.Focus();
            //Keyboard.Focus(TextEditor);
            //FocusManager.SetFocusedElement(TextEditor, TextEditor);

            Mouse.Capture(TextEditor, CaptureMode.SubTree);
            return true;
        }
        else
        {
            Debug.Fail("理论上一定能命中成功");
            return false;
        }
    }

    public virtual void OnMouseEnter(MouseEventArgs e)
    {
        //Debug.WriteLine("TextEditor_MouseEnter");
        UpdateCursorView();
    }

    public virtual void OnLostMouseCapture(MouseEventArgs e)
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
    public virtual void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            SwitchOvertypeMode();
            e.Handled = true;
            return;
        }
        else if (e.Key == Key.Enter)
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
    public virtual void OnTextInput(TextCompositionEventArgs e)
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

    #region 方向键

    public virtual partial void MoveCaret(CaretMoveType type)
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
}
