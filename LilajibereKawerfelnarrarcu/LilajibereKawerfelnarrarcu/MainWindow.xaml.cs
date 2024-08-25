using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Input;
using Microsoft.UI;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace LilajibereKawerfelnarrarcu;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        RootGrid.PointerPressed += RootGrid_PointerPressed; ;
        RootGrid.PointerMoved += RootGrid_PointerMoved; ;
        RootGrid.PointerReleased += RootGrid_PointerReleased; ;
    }

    readonly record struct PointInfo(double X, double Y, double Width, double Height, Border Border, TextBlock TextBlock);

    private readonly Dictionary<uint /*Id*/, PointInfo> _dictionary = [];

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(null);
        var position = currentPoint.Position;
        (bool success, double width, double height) = GetSize(currentPoint);

        Border border = new Border
        {
            Background = new SolidColorBrush(Colors.Gray)
            {
                Opacity = 0.5
            },
            Width = Math.Max(5, width),
            Height = Math.Max(5, height),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new TranslateTransform()
            {
                X = position.X,
                Y=position.Y
            },
        };
        RootGrid.Children.Add(border);
        var textBlock = new TextBlock()
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.Wrap,
            RenderTransform = new TranslateTransform()
            {
                X = position.X,
                Y = position.Y
            },
        };
        RootGrid.Children.Add(textBlock);

        _dictionary[e.Pointer.PointerId] = new PointInfo(position.X, position.Y, width, height, border, textBlock);
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dictionary.TryGetValue(e.Pointer.PointerId, out var info))
        {
            var currentPoint = e.GetCurrentPoint(null);
            var position = currentPoint.Position;
            var (success, width, height) = GetSize(currentPoint);

            string? errorMessage = null;
            if (double.IsNaN(width))
            {
                errorMessage += "Origin width is NaN\r\n";
            }
            if (double.IsNaN(height))
            {
                errorMessage += "Origin height is NaN\r\n";
            }

            var viewWidth = width;
            var viewHeight = height;

            if (width <= 0.01)
            {
                width = info.Width;
                viewWidth = width;
            }

            if (height <= 0.01)
            {
                height = info.Height;
                viewHeight = height;
            }

            viewWidth = Math.Max(5, viewWidth);
            viewHeight = Math.Max(5, viewHeight);

            var borderRenderTransform = (TranslateTransform) info.Border.RenderTransform!;
            borderRenderTransform.X = position.X - viewWidth / 2;
            borderRenderTransform.Y = position.Y - viewHeight / 2;

            var textBlock = info.TextBlock;
            textBlock.Text = $"Id:{e.Pointer.PointerId}\r\nX: {position.X}, Y: {position.Y}\r\nWidth: {width:F2}, Height: {height:F2}\r\n{errorMessage}";

            var textBlockRenderTransform = (TranslateTransform) textBlock.RenderTransform!;
            textBlockRenderTransform.X = position.X;
            textBlockRenderTransform.Y = position.Y;

            _dictionary[e.Pointer.PointerId] = info with
            {
                X = position.X,
                Y = position.Y,

                Width = width,
                Height = height
            };
        }
    }

    private async void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dictionary.TryGetValue(e.Pointer.PointerId, out var info))
        {
            RootGrid.Children.Remove(info.Border);
            info.TextBlock.Text = "IsUp=true\r\n" + info.TextBlock.Text;
            info.TextBlock.Opacity = 0.9;

            await Task.Delay(TimeSpan.FromSeconds(5));
            info.TextBlock.Foreground = new SolidColorBrush(Colors.Black);
            info.TextBlock.Opacity = 0.5;
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                info.TextBlock.Opacity = (5 - i) * 0.1;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
            RootGrid.Children.Remove(info.TextBlock);
        }
    }

    private (bool Success, double Width, double Height) GetSize(PointerPoint currentPoint)
    {
        try
        {
            var d = currentPoint.Properties;
            Rect contactRect = d.ContactRect;

            return (Success: true, contactRect.Width, contactRect.Height);
        }
        catch
        {
            return (Success: false, 0, 0);
        }
    }
}
