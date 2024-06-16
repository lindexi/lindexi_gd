using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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
            skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
            e.Canvas.DrawRect((float) _currentPosition.X, (float) _currentPosition.Y, 10, 10, skPaint);
        }
    }

    private void SkXamlCanvas_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {

    }
    
    private string _additionText = "";

    private void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _start = true;

        var currentPoint = e.GetCurrentPoint(this);
        _additionText = "按下" + currentPoint.PointerId;
        _inputDictionary[currentPoint.PointerId] = (currentPoint.Position, false);
        Output();
    }

    private void UIElement_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        Point position = e.GetCurrentPoint(this).Position;
        _currentPosition = position;
        this.InvalidateArrange();

        var currentPoint = e.GetCurrentPoint(this);
        _inputDictionary[currentPoint.PointerId] = (currentPoint.Position, false);
        Output();
    }

    private void UIElement_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(this);
        _additionText = "抬起" + currentPoint.PointerId;
        _inputDictionary[currentPoint.PointerId] = (currentPoint.Position, true);

        Output();
    }

    private void Output()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(_additionText);
        
        foreach (var keyValuePair in _inputDictionary)
        {
            if (keyValuePair.Value.IsUp)
            {
                continue;
            }

            stringBuilder.AppendLine($"{keyValuePair.Key.ToString().PadLeft(2)} : {keyValuePair.Value.Position.X:0.00},{keyValuePair.Value.Position.Y:0.00}");
        }

        foreach (var keyValuePair in _inputDictionary)
        {
            if (!keyValuePair.Value.IsUp)
            {
                continue;
            }

            stringBuilder.AppendLine($"{keyValuePair.Key.ToString().PadLeft(2)} : {keyValuePair.Value.Position.X:0.00},{keyValuePair.Value.Position.Y:0.00}");
        }

        LogTextBlock.Text = stringBuilder.ToString();
    }

    private readonly Dictionary<uint, (Point Position, bool IsUp)> _inputDictionary = [];

    private bool _start;

    private Point _currentPosition;
    
    private void UIElement_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(this);
        _additionText = "进入" + currentPoint.PointerId;

        Output();
    }
    
    private void UIElement_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(this);
        _additionText = "离开" + currentPoint.PointerId;

        Output();
    }
}
