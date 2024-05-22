using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using UnoInk.X11Ink;

namespace UnoInk;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsLinux())
        {
            if (_x11InkProvider == null)
            {
                _x11InkProvider = new X11InkProvider();
                
                _x11InkProvider.Start(Window.Current!);
            }
        }
    }

    private void InkCanvas_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(InkCanvas);
        Point position = pointerPoint.Position;

        //var inkInfo = new InkInfo();
        //_inkInfoCache[e.Pointer.PointerId] = inkInfo;
        //inkInfo.PointList.Add(position);
        //DrawStroke(inkInfo);
        DrawInNative(position);

        //LogTextBlock.Text += $"按下： {e.Pointer.PointerId}\r\n";
        //LogTextBlock.Text += $"当前按下点数： {_inkInfoCache.Count} [{string.Join(',', _inkInfoCache.Keys)}]";
    }

    private void InkCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        //if (_inkInfoCache.TryGetValue(e.Pointer.PointerId, out var inkInfo))
        //{
        //    var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //    Point position = pointerPoint.Position;

        //    inkInfo.PointList.Add(position);
        //    //DrawStroke(inkInfo);
        //    DrawInNative(position);
        //}
    }

    private void InkCanvas_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        //if (_inkInfoCache.Remove(e.Pointer.PointerId, out var inkInfo))
        //{
        //    var pointerPoint = e.GetCurrentPoint(InkCanvas);
        //    Point position = pointerPoint.Position;
        //    inkInfo.PointList.Add(position);
        //    DrawStroke(inkInfo);
        //}
        
        //LogTextBlock.Text += $"抬起： {e.Pointer.PointerId}\r\n";
        //LogTextBlock.Text += $"当前按下点数： {_inkInfoCache.Count} [{string.Join(',', _inkInfoCache.Keys)}]";
    }

    //private readonly Dictionary<uint /*PointerId*/, InkInfo> _inkInfoCache = new Dictionary<uint, InkInfo>();

    //private void DrawStroke(InkInfo inkInfo)
    //{
    //    var pointList = inkInfo.PointList;
    //    if (pointList.Count < 2)
    //    {
    //        // 小于两个点的无法应用算法
    //        return;
    //    }

    //    int inkSize = 16;

    //    var inkElement = MyInkRender.CreatePath(inkInfo, inkSize);

    //    if (inkInfo.InkElement is null)
    //    {
    //        InkCanvas.Children.Add(inkElement);
    //    }
    //    else if (inkElement != inkInfo.InkElement)
    //    {
    //        RemoveInkElement(inkInfo.InkElement);
    //        InkCanvas.Children.Add(inkElement);
    //    }

    //    inkInfo.InkElement = inkElement;
    //}

    private void RemoveInkElement(FrameworkElement? inkElement)
    {
        if (inkElement != null)
        {
            InkCanvas.Children.Remove(inkElement);
        }
    }

    private X11InkProvider? _x11InkProvider;

    private void DrawInNative(Point position)
    {
        if (OperatingSystem.IsLinux())
        {
            _x11InkProvider!.Draw(position);
        }
    }
}
