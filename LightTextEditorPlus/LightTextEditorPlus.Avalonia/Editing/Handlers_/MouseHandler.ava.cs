using System.Diagnostics;
using Avalonia;
using Avalonia.Input;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Utils;

namespace LightTextEditorPlus.Editing;

internal class MouseHandler
{
    public MouseHandler(TextEditor textEditor)
    {
        textEditor.PointerPressed += TextEditor_PointerPressed;
        textEditor.PointerMoved += TextEditor_PointerMoved;
        textEditor.PointerReleased += TextEditor_PointerReleased;

        TextEditor = textEditor;
    }

    public TextEditor TextEditor { get; }
    private TextEditorCore TextEditorCore => TextEditor.TextEditorCore;

    private bool _isMouseDown;

    /// <summary>
    /// 是不是点到选择范围
    /// </summary>
    private bool _isHitSelection;

    private void TextEditor_PointerPressed(object? sender, PointerPressedEventArgs e)
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

            if (_inputGesture.ClickCount % 2 == 0)
            {
                // HandleDoubleClick
                // 默认行为是双击全选，你想选词？那就不好玩了哦
                TextEditor.TextEditorCore.SelectAll();
                // 如果想选词，可以尝试对接一下 ICU 库，或者 NLP 玄学算法
                // 请参阅：
                // [UWP WinRT 使用系统自带的分词库对字符串文本进行分词](https://blog.lindexi.com/post/UWP-WinRT-%E4%BD%BF%E7%94%A8%E7%B3%BB%E7%BB%9F%E8%87%AA%E5%B8%A6%E7%9A%84%E5%88%86%E8%AF%8D%E5%BA%93%E5%AF%B9%E5%AD%97%E7%AC%A6%E4%B8%B2%E6%96%87%E6%9C%AC%E8%BF%9B%E8%A1%8C%E5%88%86%E8%AF%8D.html )
                // [dotnet 简单使用 ICU 库进行分词和分行 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18622917 )
            }
            else if (_inputGesture.ClickCount % 2 == 1)
            {
                if (e.Handled)
                {
                    // 理论上不会进入此分支
                    return;
                }

                // HandleSingleClick
                Point position = e.GetPosition(TextEditor);
                TextEditor.TextEditorPlatformProvider.EnsureLayoutUpdated();
                if (TextEditorCore.TryHitTest(position.ToTextPoint(), out var result))
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

    private void TextEditor_PointerMoved(object? sender, PointerEventArgs e)
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

    private void TextEditor_PointerReleased(object? sender, PointerReleasedEventArgs e)
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

    #region InputGestureInfo

    private readonly InputGestureInfo _inputGesture = new InputGestureInfo();

    #endregion
}
