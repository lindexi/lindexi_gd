#nullable enable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChaigelyojeeBifakeljair;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
    }

    private void Canvas_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        var imageFile = @"C:\lindexi\Image\1.png";// 图片地址大家自己替换
        if (!File.Exists(imageFile))
        {
            // 自己换成自己的图片
            Debugger.Break();
        }

        var task = LoadImageAsync();
        args.TrackAsyncAction(task.AsAsyncAction());

        async Task LoadImageAsync()
        {
            CanvasBitmap canvasBitmap = await CanvasBitmap.LoadAsync(sender, imageFile);
            _canvasBitmap = canvasBitmap;
        }
    }

    private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (_canvasBitmap is { } canvasBitmap)
        {
            var centerX = canvasBitmap.Bounds._width / 2;
            var centerY = canvasBitmap.Bounds._height / 2;

            var transform2DEffect = new Transform2DEffect();
            transform2DEffect.Source = canvasBitmap;

            var flip = _shouldFlip ? -1 : 1;
            var matrix3X2 = Matrix3x2.CreateScale(flip, 1, new Vector2(centerX, centerY));
            transform2DEffect.TransformMatrix = matrix3X2;

            var transform2DEffect2 = new Transform2DEffect()
            {
                Source = transform2DEffect,
                TransformMatrix = Matrix3x2.CreateScale((float) (sender.ActualWidth / canvasBitmap.Bounds.Width), (float) (sender.ActualHeight / canvasBitmap.Bounds.Height))
            };

            args.DrawingSession.DrawImage(transform2DEffect2);
        }
    }

    private CanvasBitmap? _canvasBitmap;

    private void MainWindow_OnClosed(object sender, WindowEventArgs args)
    {
        Canvas.RemoveFromVisualTree();
    }

    private bool _shouldFlip = false;

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        _shouldFlip = (sender as ToggleButton)?.IsChecked is true;
        Canvas.Invalidate();
    }
}
