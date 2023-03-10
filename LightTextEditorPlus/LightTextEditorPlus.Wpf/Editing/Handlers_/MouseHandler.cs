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
using System.Windows;
using LightTextEditorPlus.Utils;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using Size = System.Windows.Size;

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

/// <summary>
/// 用于处理鼠标点击信息的辅助类
/// </summary>
internal class InputGestureInfo
{
    public int ClickCount { get; private set; }

    /// <summary>
    /// 记录一次点击，同时会结合上一次点击更改ClickCount，以便判断是否为一次有效双击
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void RecordDown(IInputElement sender, MouseButtonEventArgs e)
    {
        var doubleClickTime = Win32.User32.GetDoubleClickTime();
        var doubleClickSize = new Size(
            Win32.User32.GetSystemMetrics(Win32.User32.SystemMetric.SM_CXDOUBLECLK),
            Win32.User32.GetSystemMetrics(Win32.User32.SystemMetric.SM_CYDOUBLECLK));

        var position = e.GetPosition(sender);
            //TextHelperInner.CorrectPosition(e, (UIElement) sender);
        var timestamp = e.Timestamp;

        if (timestamp - _lastClickTime > doubleClickTime || !_lastClickRect.Contains(position))
        {
            /* 以下是 bing 给的计算代码
            // 假设 input1 和 input2 是两个 InputEventArgs 对象
            int timestamp1 = input1.Timestamp;
            int timestamp2 = input2.Timestamp;

            // 如果 timestamp1 和 timestamp2 都是正数或都是负数，直接相减即可
            if ((timestamp1 >= 0 && timestamp2 >= 0) || (timestamp1 < 0 && timestamp2 < 0))
            {
                int diff = Math.Abs(timestamp2 - timestamp1); // 求绝对值
                Console.WriteLine("两次输入之间的时间差是 {0} 毫秒", diff);
            }
            // 如果 timestamp1 是正数而 timestamp2 是负数，说明 Timestamp 的值已经重新启动了
            else if (timestamp1 >= 0 && timestamp2 < 0)
            {
                // 先将 timestamp1 和 Int32.MaxValue 相减，再将结果和 Int32.MinValue 相减，最后再加上 timestamp2
                int diff = Math.Abs((Int32.MaxValue - timestamp1) + (Int32.MinValue - timestamp2));
                Console.WriteLine("两次输入之间的时间差是 {0} 毫秒", diff);
            }
            // 如果 timestamp1 是负数而 timestamp2 是正数，说明 Timestamp 的值已经重新启动了
            else if (timestamp1 < 0 && timestamp2 >= 0)
            {
                // 先将 Int32.MaxValue 和 Int32.MinValue 相减，再将结果和 -timestamp1 相加，最后再加上 -timestamp2
                int diff = Math.Abs((Int32.MaxValue - Int32.MinValue) + (-timestamp1) + (-timestamp2));
                Console.WriteLine("两次输入之间的时间差是 {0} 毫秒", diff);
            }
             */
            ClickCount = 0;
        }

        ClickCount++;
        _lastClickTime = timestamp;
        _lastClickRect = new Rect(position, new Size());
        _lastClickRect.Inflate(doubleClickSize.Width / 2, doubleClickSize.Height / 2);
    }

    ///// <summary>
    ///// 记录一次点击，同时会结合上一次点击更改ClickCount，以便判断是否为一次有效双击
    ///// </summary>
    //public void RecordDown(Point position, Input.DeviceType type)
    //{
    //    int threshold = GetThresholdSquared(type);
    //    const double doubleClickTime = 500;
    //    var timestamp = Environment.TickCount;

    //    if (timestamp - _lastClickTime > doubleClickTime || (_lastPosition - position).LengthSquared > threshold)
    //    {
    //        ClickCount = 0;
    //    }

    //    ClickCount++;
    //    _lastClickTime = timestamp;
    //    _lastPosition = position;
    //}

    //private int GetThresholdSquared(Input.DeviceType type)
    //{
    //    switch (type)
    //    {
    //        case DeviceType.Mouse:
    //            return 1;
    //        case DeviceType.Stylus:
    //            return 16;
    //        case DeviceType.Touch:
    //            return 100;
    //        default:
    //            throw new ArgumentOutOfRangeException(nameof(type), type, null);
    //    }
    //}

    private Rect _lastClickRect;
    private int _lastClickTime = int.MaxValue;
    //private Point _lastPosition;
}


