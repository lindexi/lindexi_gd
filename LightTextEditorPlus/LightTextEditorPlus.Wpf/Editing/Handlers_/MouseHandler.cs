using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Rendering;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

internal class MouseHandler
{
    public MouseHandler(TextEditor textEditor)
    {
        TextEditor = textEditor;

        textEditor.TextEditorCore.ArrangingTypeChanged += (_, _) => UpdateCursor();
        textEditor.CursorStylesChanged += (_, _) => UpdateCursor();

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
                if (TextEditorCore.TryHitTest(position.ToPoint(), out var result))
                {
                    var endOffset = result.HitCaretOffset;
                    TextEditorCore.CurrentSelection = new Selection(startOffset, endOffset);
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

    private void TextEditor_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMouseDown)
        {
            if (_isHitSelection)
            {
                var position = e.GetPosition(TextEditor);
                if (TextEditorCore.TryHitTest(position.ToPoint(), out var result))
                {
                    TextEditorCore.CurrentCaretOffset = result.HitCaretOffset;
                }
            }
        }

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
            _isMouseDown = false;
            Mouse.Capture(TextEditor, CaptureMode.None);
        }
    }

    private TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;


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

    private void UpdateCursor()
    {
        if (TextEditor.CursorStyles is not null)
        {
            TextEditor.Cursor = TextEditor.CursorStyles.Cursor;
            return;
        }

        var cursor = TextEditor.TextEditorCore.ArrangingType switch
        {
            ArrangingType.Horizontal => Cursors.IBeam,
            ArrangingType.Mongolian or ArrangingType.Vertical => GetVerticalCursor(),
            _ => Cursors.IBeam,
        };
        TextEditor.Cursor = cursor;
    }

    #endregion

    #region InputGestureInfo

    private readonly InputGestureInfo _inputGesture = new InputGestureInfo();

    #endregion
}