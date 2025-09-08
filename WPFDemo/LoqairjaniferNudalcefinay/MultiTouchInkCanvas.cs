using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
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

    private StrokeVisual GetStrokeVisual(int id)
    {
        if (StrokeVisualList.TryGetValue(id, out var visual))
        {
            return visual;
        }

        var strokeVisual = new StrokeVisual();
        StrokeVisualList[id] = strokeVisual;
        var visualCanvas = new VisualCanvas(strokeVisual);
        Grid.Children.Add(visualCanvas);

        return strokeVisual;
    }
}


/// <summary>
///     用于显示笔迹的类
/// </summary>
public class StrokeVisual : DrawingVisual
{
    /// <summary>
    ///     创建显示笔迹的类
    /// </summary>
    public StrokeVisual() : this(new DrawingAttributes()
    {
        Color = Colors.Red,
        FitToCurve = true,
        Width = 5
    })
    {
    }

    /// <summary>
    ///     创建显示笔迹的类
    /// </summary>
    /// <param name="drawingAttributes"></param>
    public StrokeVisual(DrawingAttributes drawingAttributes)
    {
        _drawingAttributes = drawingAttributes;
    }

    private readonly DrawingAttributes _drawingAttributes;

    /// <summary>
    ///     设置或获取显示的笔迹
    /// </summary>
    public Stroke? Stroke { set; get; }

    /// <summary>
    ///     在笔迹中添加点
    /// </summary>
    /// <param name="point"></param>
    public void Add(StylusPoint point)
    {
        if (Stroke == null)
        {
            var collection = new StylusPointCollection { point };
            Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
        }
        else
        {
            Stroke.StylusPoints.Add(point);
        }
    }

    /// <summary>
    ///     重新画出笔迹
    /// </summary>
    public void Redraw()
    {
        using var dc = RenderOpen();
        Stroke?.Draw(dc);
    }
}

public class VisualCanvas : FrameworkElement
{
    protected override Visual GetVisualChild(int index)
    {
        return Visual;
    }

    protected override int VisualChildrenCount => 1;

    public VisualCanvas(DrawingVisual visual)
    {
        Visual = visual;
        AddVisualChild(visual);
    }

    public DrawingVisual Visual { get; }
}
