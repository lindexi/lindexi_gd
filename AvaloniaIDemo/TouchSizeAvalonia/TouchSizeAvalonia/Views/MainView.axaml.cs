using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace TouchSizeAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        PointerPressed += MainView_PointerPressed;
        PointerMoved += MainView_PointerMoved;
        PointerReleased += MainView_PointerReleased;
    }

    readonly record struct PointInfo(double X, double Y, double Width, double Height, Border Border, TextBlock TextBlock);

    private readonly Dictionary<int /*Id*/, PointInfo> _dictionary = [];

    private void MainView_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(null);
        var position = currentPoint.Position;
        (bool success, double width, double height) = GetSize(currentPoint);
        //if (!success)
        //{
        //    width = 5;
        //    height = 5;
        //}

        Border border = new Border
        {
            Background = new SolidColorBrush(Colors.Gray, 0.5),
            Width = Math.Max(5, width),
            Height = Math.Max(5, height),
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            RenderTransform = new TranslateTransform(position.X, position.Y),
        };
        RootGrid.Children.Add(border);

        var textBlock = new TextBlock()
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.Wrap,
            RenderTransform = new TranslateTransform(position.X, position.Y),
        };
        RootGrid.Children.Add(textBlock);

        _dictionary[e.Pointer.Id] = new PointInfo(position.X, position.Y, width, height, border, textBlock);
    }

    private void MainView_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (_dictionary.TryGetValue(e.Pointer.Id, out var info))
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
            textBlock.Text = $"Id:{e.Pointer.Id}\r\nX: {position.X}, Y: {position.Y}\r\nWidth: {width:F2}, Height: {height:F2}\r\n{errorMessage}";

            var textBlockRenderTransform = (TranslateTransform) textBlock.RenderTransform!;
            textBlockRenderTransform.X = position.X;
            textBlockRenderTransform.Y = position.Y;

            _dictionary[e.Pointer.Id] = info with
            {
                X = position.X,
                Y = position.Y,

                Width = width,
                Height = height
            };
        }
    }

    private async void MainView_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (_dictionary.TryGetValue(e.Pointer.Id, out var info))
        {
            RootGrid.Children.Remove(info.Border);

            //var animation = new Animation()
            //{
            //    Duration = TimeSpan.FromSeconds(2),
            //    IterationCount = new IterationCount(1),
            //    Children =
            //    {
            //        new KeyFrame()
            //        {
            //            Setters =
            //            {
            //                new Setter(OpacityProperty,value:1),
            //            },
            //            KeyTime = TimeSpan.FromSeconds(0)
            //        },
            //        new KeyFrame()
            //        {
            //            Setters =
            //            {
            //                new Setter(OpacityProperty,value:0),
            //            },
            //            KeyTime = TimeSpan.FromSeconds(10)
            //        }
            //    }
            //};

            //animation.RunAsync(info.TextBlock).ContinueWith(_ =>
            //{
            //    RootGrid.Children.Remove(info.TextBlock);
            //});

            //info.TextBlock.Foreground = Brushes.Gray;
            info.TextBlock.Text = "IsUp=true\r\n" + info.TextBlock.Text;
            info.TextBlock.Opacity = 0.9;

            await Task.Delay(TimeSpan.FromSeconds(5));
            info.TextBlock.Foreground = Brushes.Black;
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
            dynamic d = currentPoint.Properties;
            Rect contactRect = d.ContactRect;

            return (Success: true, contactRect.Width, contactRect.Height);
        }
        catch
        {
            return (Success: false, 0, 0);
        }
    }
}
