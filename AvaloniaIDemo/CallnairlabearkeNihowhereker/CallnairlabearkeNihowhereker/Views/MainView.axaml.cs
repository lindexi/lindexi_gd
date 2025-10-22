using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Threading;

namespace CallnairlabearkeNihowhereker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

}

class Foo : Control, IScrollable
//,IScrollSnapPointsInfo
{
    public Foo()
    {
        Height = 10000;
    }

    public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
    {
        throw new NotImplementedException();
    }

    public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
    {
        throw new NotImplementedException();
    }

    public bool AreHorizontalSnapPointsRegular { get; set; }
    public bool AreVerticalSnapPointsRegular { get; set; }
    public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
    public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;
    public Size Extent { get; set; }
    public Vector Offset { get; set; }
    public Size Viewport { get; set; }
}