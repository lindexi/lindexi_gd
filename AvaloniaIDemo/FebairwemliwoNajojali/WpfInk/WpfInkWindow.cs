using RuhuyagayBemkaijearfear;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using InkBase;

namespace WpfInk;

public class WpfInkWindow : PerformanceDesktopTransparentWindow, IWpfInkLayer
{
    public WpfInkWindow()
    {
        Title = "WpfInk";
        //AllowsTransparency = true;123
        WindowStyle = WindowStyle.None;
        Background = new SolidColorBrush(new Color()
        {
            A = 0x5c,
            R = 0x56,
            G = 0x56,
            B = 0x56
        });
        WindowState = WindowState.Maximized;
        SetTransparentHitThrough();

        WpfForAvaloniaInkingAccelerator.Instance.InkLayer = this;

        //Content = new Grid
        //{
        //    Children =
        //    {
        //        new Button()
        //        {
        //            Width = 100,
        //            Height = 30,
        //            HorizontalAlignment = HorizontalAlignment.Right,
        //            VerticalAlignment = VerticalAlignment.Top,
        //            Content = "退出"
        //        }.Do(b =>
        //        {
        //            b.Click += (s, e) =>
        //            {
        //                Environment.Exit(0);
        //            };
        //        }),
        //    }
        //};
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(Brushes.Red, null, new Rect(10, 10, 100, 100));
    }

    public void Down(InkPoint screenPoint)
    {
        throw new NotImplementedException();
    }

    public void Move(InkPoint screenPoint)
    {
        throw new NotImplementedException();
    }

    public void Up(InkPoint screenPoint)
    {
        throw new NotImplementedException();
    }

    public event EventHandler<SkiaStroke>? StrokeCollected;
    public void HideStroke(SkiaStroke skiaStroke)
    {
        throw new NotImplementedException();
    }
}

static class UIExtension
{
    public static T Do<T>(this T t, Action<T> action)
    {
        action(t);
        return t;
    }
}
