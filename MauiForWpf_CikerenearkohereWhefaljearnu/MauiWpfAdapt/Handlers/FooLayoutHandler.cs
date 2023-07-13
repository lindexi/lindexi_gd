using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Color = System.Windows.Media.Color;
using Rect = Microsoft.Maui.Graphics.Rect;
using Size = Microsoft.Maui.Graphics.Size;

namespace MauiWpfAdapt.Handlers;

class FooLayoutHandler : LayoutHandler
{
    public FooLayoutHandler() : base(new PropertyMapper<ILayout, ILayoutHandler>(Mapper)
    {
        [nameof(ILayout.Background)] = MapFooBackground,
    })
    {
    }

    public override void PlatformArrange(Rect rect)
    {
        base.PlatformArrange(rect);
        var size = VirtualView.CrossPlatformArrange(rect);
        var platformView = (Canvas) PlatformView;
        platformView.SetValue(Canvas.LeftProperty, rect.Left);
        platformView.SetValue(Canvas.TopProperty, rect.Top);
        platformView.Width = size.Width;
        platformView.Height = size.Height;
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        return VirtualView.CrossPlatformMeasure(widthConstraint, heightConstraint);
    }

    private static void MapFooBackground(ILayoutHandler layoutHandler, ILayout layout)
    {
        var fooLayoutHandler = (FooLayoutHandler) layoutHandler;
        var platformView = (Canvas) fooLayoutHandler.PlatformView;
        if (layout.Background is SolidPaint solidPaint)
        {
            solidPaint.Color.ToRgba(out var r, out var g, out var b, out var a);
            platformView.Background = new System.Windows.Media.SolidColorBrush()
            {
                Color = new Color()
                {
                    A = a,
                    R = r,
                    G = g,
                    B = b,
                }
            };
        }
        else
        {
            platformView.Background = null;
        }
    }

    public override void SetVirtualView(IView view)
    {
        base.SetVirtualView(view);

        var virtualView = (ILayout) VirtualView;
        var platformView = (Canvas) PlatformView;

        platformView.Children.Clear();

        foreach (var child in virtualView.OrderBy(v => v.ZIndex))
        {
            platformView.Children.Add((UIElement) child.ToPlatform(MauiContext));
        }
    }

    protected override object CreatePlatformView()
    {
        return new Canvas();
    }
}