using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace KearkemnerwhayneqiChaywibelfo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}

class F1 : FrameworkElement
{
    public F1()
    {
        Width = 500;
        Height = 500;

        F2 = new F2();

        Loaded += F1_Loaded;

        AddLogicalChild(F2);
        AddVisualChild(F2);
    }

    private F2 F2 { get; }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => F2;

    protected override Size MeasureOverride(Size availableSize)
    {
        Debug.WriteLine("F1 MeasureOverride");
        F2.Measure(availableSize);
        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        F2.Arrange(new Rect(new Point(), finalSize));
        return base.ArrangeOverride(finalSize);
    }

    private void F1_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(nameof(F1_Loaded));
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return base.HitTestCore(hitTestParameters);
    }
}

class F2 : FrameworkElement
{
    public F2()
    {
        Width = 500;
        Height = 500;

        Loaded += F2_Loaded;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Debug.WriteLine("F2 MeasureOverride");
        return base.MeasureOverride(availableSize);
    }

    private void F2_Loaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(nameof(F2_Loaded));
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return base.HitTestCore(hitTestParameters);
    }
}