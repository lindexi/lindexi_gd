using System.Windows;
using System.Windows.Input;

namespace LightTextEditorPlus.Utils;

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