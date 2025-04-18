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

public class WpfInkWindow : PerformanceDesktopTransparentWindow
{
    public WpfInkWindow()
    {
        Title = "WpfInk";
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

        _wpfInkLayer = new WpfInkLayer(this);
        WpfForAvaloniaInkingAccelerator.Instance.InkLayer = _wpfInkLayer;

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

    private readonly WpfInkLayer _wpfInkLayer;

    protected override void OnRender(DrawingContext drawingContext)
    {
        _wpfInkLayer.Render(drawingContext);
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
