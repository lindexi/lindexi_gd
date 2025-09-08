using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LoqairjaniferNudalcefinay;

public class MultiTouchInkCanvas : Grid
{
    public MultiTouchInkCanvas()
    {
        // 只是为了命中测试，设置背景是透明，这样就能收到输入
        Background = Brushes.Transparent;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        // 使用 StylusMove 事件的速度将会比较慢
        StylusDown += MultiTouchInkCanvas_StylusDown;
        StylusMove += MultiTouchInkCanvas_StylusMove;
        StylusUp += MultiTouchInkCanvas_StylusUp;
    }

    private void MultiTouchInkCanvas_StylusDown(object sender, StylusDownEventArgs e)
    {
        var strokeVisual = new StrokeVisual();
        StrokeVisualList[e.StylusDevice.Id] = strokeVisual;
        var visualCanvas = new VisualCanvas(strokeVisual);
        Grid.Children.Add(visualCanvas);
        Draw(e, strokeVisual);
    }

    private void MultiTouchInkCanvas_StylusMove(object sender, StylusEventArgs e)
    {
        if (!StrokeVisualList.TryGetValue(e.StylusDevice.Id, out var strokeVisual))
        {
            return;
        }

        Draw(e, strokeVisual);
    }

    private void Draw(StylusEventArgs e, StrokeVisual strokeVisual)
    {
        var stylusPointCollection = e.GetStylusPoints(this);
        foreach (var stylusPoint in stylusPointCollection)
        {
            strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y));
        }

        strokeVisual.Redraw();
    }

    private void MultiTouchInkCanvas_StylusUp(object sender, StylusEventArgs e)
    {
        StrokeVisualList.Remove(e.StylusDevice.Id);
    }

    // 其实不使用 Grid 而使用自己定制的 Panel 的性能能更好，但是这里只是给例子而已
    public Grid Grid => this;

    // 如果后续性能优化，使用触摸线程拿到输入，那么记得鼠标和触摸是两个不同线程，不能使用字典
    private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();
}