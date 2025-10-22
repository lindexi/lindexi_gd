using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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

class Foo : Control, ILogicalScrollable
//,IScrollSnapPointsInfo
{
    public Foo()
    {
    }

    /// <summary>
    /// 总控件大小
    /// </summary>
    public Size Extent { get; set; } = new Size(100, 10000);

    /// <summary>
    /// 当前偏移量
    /// </summary>
    public Vector Offset { get; set; }

    /// <summary>
    /// 当前视口大小，即可见区域大小
    /// </summary>
    /// Viewport 与 Extent 的比例就是滚动条的比例，比例越小，滚动条越短
    public Size Viewport { get; set; } = new Size(100, 1);

    public bool BringIntoView(Control target, Rect targetRect)
    {
        return false;
    }

    public Control? GetControlInDirection(NavigationDirection direction, Control? from)
    {
        return null;
    }

    public void RaiseScrollInvalidated(EventArgs e)
    {
        ScrollInvalidated?.Invoke(this, e);
    }

    public bool CanHorizontallyScroll { get; set; }
    public bool CanVerticallyScroll { get; set; }
    public bool IsLogicalScrollEnabled { get; set; } = true;

    /// <summary>
    /// 一次滚动的大小
    /// </summary>
    public Size ScrollSize { get; set; } = new Size(100, 2);

    /// <summary>
    /// 一次页面滚动的大小
    /// </summary>
    public Size PageScrollSize { get; set; } = new Size(100, 100);

    public event EventHandler? ScrollInvalidated;
}