using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.Input.Touch;

using Point = System.Drawing.Point;

namespace LurlowhairwiwenawhiFeajobaihere;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 准备在后台线程中输出日志等逻辑写的简单代码
        _channel = Channel.CreateUnbounded<Action>();

        SourceInitialized += OnSourceInitialized;

        //StylusMove += MainWindow_StylusMove;
        //StylusUp += MainWindow_StylusUp;
        //TouchMove += MainWindow_TouchMove;
        //TouchUp += MainWindow_TouchUp;
        //StylusPlugIns.Add(new F());

        Foo();
    }

    private Channel<Action> _channel;

    private async void Foo()
    {
        while (true)
        {
            var action = await _channel.Reader.ReadAsync();
            action();
        }
    }

    //private List<Point2D> _wpfPointList = [];
    //private List<Point2D> _pointerPointList = [];

    //private bool _isWpfUp;
    //private bool _isPointerUp;

    private void MainWindow_TouchMove(object? sender, TouchEventArgs e)
    {
        var touchPoint = e.GetTouchPoint(RootGrid);
        var strokeVisual = GetStrokeVisual((uint) e.TouchDevice.Id);
        strokeVisual.Add(new StylusPoint(touchPoint.Position.X, touchPoint.Position.Y));
        strokeVisual.Redraw();
        Console.WriteLine($"WPF {e.TouchDevice.Id} XY={touchPoint.Position.X},{touchPoint.Position.Y}");

        //if (!_isWpfUp)
        //{
        //    _wpfPointList.Add(new Point2D(touchPoint.Position.X, touchPoint.Position.Y));
        //}
    }

    private void MainWindow_TouchUp(object? sender, TouchEventArgs e)
    {
        StrokeVisualList.Remove((uint) e.TouchDevice.Id);
        //_isWpfUp = true;
        //Output();
    }

    private void MainWindow_StylusMove(object sender, StylusEventArgs e)
    {
        var position = e.GetPosition(RootGrid);
        var strokeVisual = GetStrokeVisual((uint) e.StylusDevice.Id);
        strokeVisual.Add(new StylusPoint(position.X, position.Y));
        strokeVisual.Redraw();
    }

    private void MainWindow_StylusUp(object sender, StylusEventArgs e)
    {
        StrokeVisualList.Remove((uint) e.StylusDevice.Id);
    }

    private Dictionary<uint, StrokeVisual> StrokeVisualList { get; } = new Dictionary<uint, StrokeVisual>();

    private StrokeVisual GetStrokeVisual(uint id)
    {
        if (StrokeVisualList.TryGetValue(id, out var visual))
        {
            return visual;
        }

        var strokeVisual = new StrokeVisual();
        StrokeVisualList[id] = strokeVisual;
        var visualCanvas = new VisualCanvas(strokeVisual)
        {
            IsHitTestVisible = false
        };
        RootGrid.Children.Add(visualCanvas);

        return strokeVisual;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        PInvoke.RegisterTouchWindow(new HWND(hwnd), 0);
        //PInvoke.RegisterTouchWindow(new HWND(hwnd), REGISTER_TOUCH_WINDOW_FLAGS.TWF_WANTPALM);

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
    }

    private void AsyncConsole(string message)
    {
        message = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        _channel.Writer.TryWrite(() => Console.WriteLine(message));
    }

    private Stopwatch _touchStopwatch = new Stopwatch();
    private Stopwatch _pointerStopwatch = new Stopwatch();

    private int _touchCount;
    private int _pointerCount;

    private void CountTouch()
    {
        _touchCount++;
        if (_touchStopwatch.IsRunning)
        {
            if (_touchStopwatch.Elapsed > TimeSpan.FromSeconds(1))
            {
                var fps = _touchCount / _touchStopwatch.Elapsed.TotalSeconds;

                AsyncConsole($"Touch 频次： {fps}");

                _touchStopwatch.Restart();
                _touchCount = 0;
            }
        }
        else
        {
            _touchStopwatch.Restart();
        }
    }

    private void CountPointer()
    {
        _pointerCount++;

        if (_pointerStopwatch.IsRunning)
        {
            if (_pointerStopwatch.Elapsed > TimeSpan.FromSeconds(1))
            {
                var fps = _pointerCount / _pointerStopwatch.Elapsed.TotalSeconds;

                AsyncConsole($"Pointer 频次： {fps}");

                _pointerStopwatch.Restart();
                _pointerCount = 0;
            }
        }
        else
        {
            _pointerStopwatch.Restart();
        }
    }

    private unsafe IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int WM_POINTERDOWN = 0x0246;
        const int WM_POINTERUPDATE = 0x0245;
        const int WM_POINTERUP = 0x0247;

        if (msg is WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP)
        {
            // 为什么这里可以不用判断 win8 以上？原因是 win8 以下不会走到这里，不会收到 WM_POINTERDOWN 等消息
            var pointerId = (uint) (ToInt32(wparam) & 0xFFFF);
            PInvoke.GetPointerTouchInfo(pointerId, out var info);
            POINTER_INFO pointerInfo = info.pointerInfo;

            global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
            global::Windows.Win32.Foundation.RECT displayRect = default;

            PInvoke.GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

            uint count = 0;
            PInvoke.GetPointerDeviceProperties(pointerInfo.sourceDevice, &count, null);
            var properties = stackalloc POINTER_DEVICE_PROPERTY[(int) count];
            PInvoke.GetPointerDeviceProperties(pointerInfo.sourceDevice, &count, properties);
            
            POINTER_DEVICE_PROPERTY widthProperty = default;
            POINTER_DEVICE_PROPERTY heightProperty = default;

            for (int i = 0; i < count; i++)
            {
                POINTER_DEVICE_PROPERTY pointerDeviceProperty = properties[i];
                var widthId = 0x48;
                var heightId = 0x48;

                if (pointerDeviceProperty.usageId == widthId)
                {
                    widthProperty = pointerDeviceProperty;
                }
                else if (pointerDeviceProperty.usageId == heightId)
                {
                    heightProperty = pointerDeviceProperty;
                }
            }

            Console.WriteLine($"WidthProperty={ToString(widthProperty)}");
            Console.WriteLine($"HeightProperty={ToString(heightProperty)}");

            string ToString(POINTER_DEVICE_PROPERTY property)
            {
                return $"UsageId={property.usageId}; Unit={property.unit}; PhysicalMin={property.physicalMin}; PhysicalMax={property.physicalMax}; LogicalMin={property.logicalMin}; LogicalMax={property.logicalMax}; UnitExponent={property.unitExponent}";
            }

            if ((info.touchMask & PInvoke.TOUCH_MASK_CONTACTAREA )!= 0)
            {
                double width = info.rcContact.Width;
                double height = info.rcContact.Height;

                var w = width / displayRect.Width * pointerDeviceRect.Width;
                var h = height / displayRect.Height * pointerDeviceRect.Height;


                //widthProperty.physicalMax
            }

            //Console.WriteLine($"PointerDeviceRect={pointerDeviceRect.X},{pointerDeviceRect.Y} WH={pointerDeviceRect.Width},{pointerDeviceRect.Height} DisplayRect={displayRect.X},{displayRect.Y} WH={displayRect.Width},{displayRect.Height}");

            //Console.WriteLine($"Id={pointerId}; PixelLocation={pointerInfo.ptPixelLocation.X},{pointerInfo.ptPixelLocation.Y}; Raw={pointerInfo.ptPixelLocationRaw.X},{pointerInfo.ptPixelLocationRaw.Y} Same={pointerInfo.ptPixelLocation== pointerInfo.ptPixelLocationRaw}");

            // 从 ptPixelLocation 拿到的是丢失精度的点，像素为单位。如果在精度稍微高的触摸屏下，将会有明显的锯齿效果
            // 如果真要使用此点，可配合 [WPF 记一个特别简单的点集滤波平滑方法 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18387840 ) 提供的方法优化
            var point = pointerInfo.ptPixelLocation;
            PInvoke.ScreenToClient(new HWND(hwnd), ref point);

            //AsyncConsole($"Pointer {pointerId} XY={point.X},{point.Y} Himetric={pointerInfo.ptHimetricLocationRaw.X},{pointerInfo.ptHimetricLocationRaw.Y}");

            CountPointer();

            var point2D = new Point2D(point.X, point.Y);

            // 如果想要获取比较高精度的触摸点，可以使用 ptHimetricLocationRaw 字段
            // 由于 ptHimetricLocationRaw 采用的是 pointerDeviceRect 坐标系，需要转换到屏幕坐标系
            // 转换方法就是先将 ptHimetricLocationRaw 的 X 坐标，压缩到 [0-1] 范围内，然后乘以 displayRect 的宽度，再加上 displayRect 的 left 值，即得到了屏幕坐标系的 X 坐标。压缩到 [0-1] 范围内的方法就是除以 pointerDeviceRect 的宽度
            // 为什么需要加上 displayRect.left 的值？考虑多屏的情况，屏幕可能是副屏
            // Y 坐标同理
            point2D = new Point2D(
                pointerInfo.ptHimetricLocationRaw.X / (double) pointerDeviceRect.Width * displayRect.Width +
                displayRect.left,
                pointerInfo.ptHimetricLocationRaw.Y / (double) pointerDeviceRect.Height * displayRect.Height +
                displayRect.top);
            //var p = PointFromScreen(new System.Windows.Point(point2D.X, point2D.Y));
            //point2D = new Point2D(p.X, p.Y);

            // 获取到的屏幕坐标系的点，需要转换到 WPF 坐标系
            // 转换过程的两个重点：
            // 1. 底层 ClientToScreen 只支持整数类型，直接转换会丢失精度。即使是 WPF 封装的 PointFromScreen 或 PointToScreen 方法也会丢失精度
            // 2. 需要进行 DPI 换算，必须要求 DPI 感知

            // 先测量窗口与屏幕的偏移量，这里直接取 0 0 点即可，因为这里获取到的是虚拟屏幕坐标系，不需要考虑多屏的情况
            var screenTranslate = new Point(0, 0);
            PInvoke.ClientToScreen(new HWND(hwnd), ref screenTranslate);
            // 获取当前的 DPI 值
            var dpi = VisualTreeHelper.GetDpi(this);
            // 先做平移，再做 DPI 换算
            point2D = new Point2D(point2D.X - screenTranslate.X, point2D.Y - screenTranslate.Y);
            point2D = new Point2D(point2D.X / dpi.DpiScaleX, point2D.Y / dpi.DpiScaleY);

            // 此时拿到的 point2D 就是 WPF 坐标系的点了
            Console.WriteLine($"{point2D.X},{point2D.Y}");

            //if (!_isPointerUp)
            //{
            //    _pointerPointList.Add(point2D);
            //}

            //if (msg == WM_POINTERUP)
            //{
            //    _isPointerUp = true;
            //    Output();
            //}

            if (msg == WM_POINTERUPDATE)
            {
                var strokeVisual = GetStrokeVisual(pointerId);
                strokeVisual.Add(new StylusPoint(point2D.X, point2D.Y));
                strokeVisual.Redraw();
            }
            else if (msg == WM_POINTERUP)
            {
                StrokeVisualList.Remove(pointerId);
            }
        }
        else if ((uint) msg is PInvoke.WM_TOUCH)
        {
            // 这里的 WM_TOUCH 是从 Pointer 消息转换过来的
            // 开启 WM_Pointer 支持之后，不需要禁用实时触摸即可收到 WM_TOUCH 消息
            var touchInputCount = wparam.ToInt32();

            var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
            if (PInvoke.GetTouchInputInfo(new HTOUCHINPUT(lparam), (uint) touchInputCount, pTouchInputs,
                    sizeof(TOUCHINPUT)))
            {
                for (var i = 0; i < touchInputCount; i++)
                {
                    var touchInput = pTouchInputs[i];
                    var point = new Point(touchInput.x / 100, touchInput.y / 100);
                    PInvoke.ScreenToClient(new HWND(hwnd), ref point);

                    //AsyncConsole($"Touch {touchInput.dwID} XY={point.X}, {point.Y}");
                    CountTouch();
                    break;

                    //if (touchInput.dwFlags.HasFlag(TOUCHEVENTF_FLAGS.TOUCHEVENTF_MOVE))
                    //{
                    //    var strokeVisual = GetStrokeVisual(touchInput.dwID);
                    //    strokeVisual.Add(new StylusPoint(point.X, point.Y));
                    //    strokeVisual.Redraw();
                    //}
                    //else if (touchInput.dwFlags.HasFlag(TOUCHEVENTF_FLAGS.TOUCHEVENTF_UP))
                    //{
                    //    StrokeVisualList.Remove(touchInput.dwID);
                    //}
                }

                PInvoke.CloseTouchInputHandle(new HTOUCHINPUT(lparam));
            }
        }

        return IntPtr.Zero;
    }

    //private void Output()
    //{
    //    if (_isWpfUp && _isPointerUp)
    //    {
    //        Console.WriteLine($"WPF 触摸点数量： {_wpfPointList.Count} Pointer点数量： {_pointerPointList.Count}");

    //        for (int i = 0; i < _wpfPointList.Count || i < _pointerPointList.Count; i++)
    //        {
    //            string message;
    //            if (i < _wpfPointList.Count)
    //            {
    //                message = $"{_wpfPointList[i].X:0000},{_wpfPointList[i].Y:0000}";
    //            }
    //            else
    //            {
    //                message = "    ,    ";
    //            }

    //            message += " | ";
    //            if (i < _pointerPointList.Count)
    //            {
    //                message += $"{_pointerPointList[i].X:0000},{_pointerPointList[i].Y:0000}";
    //            }

    //            Console.WriteLine(message);
    //        }
    //    }
    //}

    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);

    private void CheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        StrokeVisual.ShouldReCreatePoint = (sender as CheckBox)?.IsChecked ?? false;
    }
}

readonly record struct Point2D(double X, double Y);

//class F : StylusPlugIn
//{
//    protected override void OnStylusMove(RawStylusInput rawStylusInput)
//    {
//        base.OnStylusMove(rawStylusInput);
//    }
//}