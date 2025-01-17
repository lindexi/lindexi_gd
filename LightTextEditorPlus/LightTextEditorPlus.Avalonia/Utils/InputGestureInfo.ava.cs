using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Input;
using LightTextEditorPlus.Core.Primitive;

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
    public void RecordDown(InputElement sender, PointerPressedEventArgs e)
    {
        uint doubleClickTime = 500;
        int doubleClickWidth = 4;
        int doubleClickHeight = 4;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            doubleClickTime = Win32.User32.GetDoubleClickTime();
            doubleClickWidth = Win32.User32.GetSystemMetrics(Win32.User32.SystemMetric.SM_CXDOUBLECLK);
            doubleClickHeight = Win32.User32.GetSystemMetrics(Win32.User32.SystemMetric.SM_CYDOUBLECLK);
        }

        var position = e.GetPosition(sender).ToTextPoint();
        var timestamp = e.Timestamp;

        if (timestamp - _lastClickTime > doubleClickTime || !_lastClickRect.Contains(position))
        {
            ClickCount = 0;
        }

        ClickCount++;
        _lastClickTime = timestamp;
        _lastClickRect = new TextRect(position, new TextSize());
        _lastClickRect.Inflate(doubleClickWidth / 2d, doubleClickHeight / 2d);
    }

    private TextRect _lastClickRect;
    private ulong _lastClickTime = ulong.MaxValue;
}
