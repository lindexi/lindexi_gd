using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JagerekukibeHelcewhalay;
/// <summary>
/// SelectionCanvas.xaml 的交互逻辑
/// </summary>
public partial class SelectionCanvas : Canvas
{
    public SelectionCanvas()
    {
        InitializeComponent();

        SetSelectionCanvas(this, this);

        SizeChanged += SelectionCanvas_SizeChanged;
    }

    private void SelectionCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
    }

    public static SelectionCanvas GetSelectionCanvas(DependencyObject obj)
    {
        return (SelectionCanvas) obj.GetValue(SelectionCanvasProperty);
    }

    public static void SetSelectionCanvas(DependencyObject obj, SelectionCanvas value)
    {
        obj.SetValue(SelectionCanvasProperty, value);
    }

    public static readonly DependencyProperty SelectionCanvasProperty =
        DependencyProperty.RegisterAttached("SelectionCanvas", typeof(SelectionCanvas), typeof(SelectionCanvas), new FrameworkPropertyMetadata(default(SelectionCanvas), FrameworkPropertyMetadataOptions.Inherits));
}

public class LeftTopThumb : FrameworkElement
{
    public LeftTopThumb()
    {
        IsManipulationEnabled = true;

        Width = 20;
        Height = 20;

        ManipulationStarting += LeftTopThumb_ManipulationStarting;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
    }

    private void LeftTopThumb_ManipulationStarting(object? sender, ManipulationStartingEventArgs e)
    {
        Manipulation.SetManipulationMode(this, ManipulationModes.Translate);
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, new Point());
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        Pen pen = new Pen(Brushes.Black, 2);
        drawingContext.DrawLine(pen, new Point(), new Point(0, 10));
        drawingContext.DrawLine(pen, new Point(), new Point(10, 0));
    }
}