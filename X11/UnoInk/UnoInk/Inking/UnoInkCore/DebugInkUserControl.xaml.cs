using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using SkiaSharp.Views.Windows;

using Windows.Foundation;
using Windows.Foundation.Collections;
using SkiaSharp;
using Uno.Skia;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UnoInk.Inking.UnoInkCore;
public sealed partial class DebugInkUserControl : UserControl
{
    public DebugInkUserControl()
    {
        this.InitializeComponent();
        
#if HAS_UNO
        var skiaVisual = SkiaVisual.CreateAndInsertTo(this);
        skiaVisual.OnDraw += SkiaVisual_OnDraw;
#endif
    }
    
    private void SkiaVisual_OnDraw(object? sender, SKSurface e)
    {
        if (_start)
        {
            using var skPaint = new SKPaint();
            skPaint.Style = SKPaintStyle.Fill;
            skPaint.Color = new SKColor((uint)Random.Shared.Next()).WithAlpha(0xFF);
            e.Canvas.DrawRect((float) _currentPosition.X, (float) _currentPosition.Y,10,10,skPaint);
        }
    }
    
    private void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {

    }
    
    private void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _start = true;
    }
    
    private void UIElement_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        Point position = e.GetCurrentPoint(this).Position;
        _currentPosition = position;
        this.InvalidateArrange();
    }
    
    private bool _start;
    private Point _currentPosition;
}
