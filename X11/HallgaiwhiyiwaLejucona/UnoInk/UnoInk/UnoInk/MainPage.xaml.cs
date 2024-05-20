using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using System.Linq;
using Microsoft.UI;

namespace UnoInk;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void InkCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(InkCanvas);
        Point position = pointerPoint.Position;

        var inkInfo = new InkInfo();
        _inkInfoCache[e.Pointer.PointerId] = inkInfo;
        inkInfo.PointList.Add(position);

        DrawStroke(inkInfo);
    }


    private void InkCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_inkInfoCache.TryGetValue(e.Pointer.PointerId, out var inkInfo))
        {
            var pointerPoint = e.GetCurrentPoint(InkCanvas);
            Point position = pointerPoint.Position;

            inkInfo.PointList.Add(position);
            DrawStroke(inkInfo);
        }
    }

    private void InkCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_inkInfoCache.Remove(e.Pointer.PointerId, out var inkInfo))
        {
            var pointerPoint = e.GetCurrentPoint(InkCanvas);
            Point position = pointerPoint.Position;
            inkInfo.PointList.Add(position);
            DrawStroke(inkInfo);
        }
    }

    private void InkCanvas_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_inkInfoCache.Remove(e.Pointer.PointerId, out var inkInfo))
        {
            RemoveInkElement(inkInfo.InkElement);
        }
    }

    private readonly Dictionary<uint /*PointerId*/, InkInfo> _inkInfoCache = new Dictionary<uint, InkInfo>();

    private void RemoveInkElement(FrameworkElement? inkElement)
    {
        if (inkElement != null)
        {
            InkCanvas.Children.Remove(inkElement);
        }
    }

    private void DrawStroke(InkInfo inkInfo)
    {
        var pointList = inkInfo.PointList;
        if (pointList.Count < 2)
        {
            // 小于两个点的无法应用算法
            return;
        }

        // 模拟笔锋

        // 用于当成笔锋的点的数量
        var tipCount = 20;

        for (int i = 0; i < pointList.Count; i++)
        {
            if ((pointList.Count - i) < tipCount)
            {
                pointList[i] = pointList[i] with
                {
                    Pressure = (pointList.Count - i) * 1f / tipCount
                };
            }
            else
            {
                pointList[i] = pointList[i] with
                {
                    Pressure = 1.0f
                };
            }
        }

        // 笔迹大小，笔迹粗细
        int inkSize = 16;

        if (DebugModeCheckBox.IsChecked is true)
        {
            // 调试模式
            var outlinePointList = MyInkRender.GetOutlinePointList(inkInfo.PointList, inkSize);

            var polygon = new Polygon();
            foreach (var point in outlinePointList)
            {
                polygon.Points.Add(point);
            }
            //path.Stroke = new SolidColorBrush(Colors.Red);
            polygon.Fill = new SolidColorBrush(Colors.Red);

            var canvas = new Canvas()
            {
                Children = { polygon }
            };

            foreach (var point in inkInfo.PointList)
            {
                var size = 6.0;
                var ellipse = new Ellipse()
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Colors.Blue)
                };
                Canvas.SetLeft(ellipse, point.Point.X - size / 2);
                Canvas.SetTop(ellipse, point.Point.Y - size / 2);

                canvas.Children.Add(ellipse);
            }

            foreach (var point in outlinePointList)
            {
                var size = 2.0;
                var ellipse = new Ellipse()
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Colors.Yellow)
                };
                Canvas.SetLeft(ellipse, point.X - size / 2);
                Canvas.SetTop(ellipse, point.Y - size / 2);

                canvas.Children.Add(ellipse);
            }

            RemoveInkElement(inkInfo.InkElement);
            InkCanvas.Children.Add(canvas);
            inkInfo.InkElement = canvas;
        }
        else
        {
            var inkElement = MyInkRender.CreatePath(inkInfo, inkSize);

            if (inkInfo.InkElement is null)
            {
                InkCanvas.Children.Add(inkElement);
            }
            else if (inkElement != inkInfo.InkElement)
            {
                RemoveInkElement(inkInfo.InkElement);
                InkCanvas.Children.Add(inkElement);
            }

            inkInfo.InkElement = inkElement;
        }
    }
}