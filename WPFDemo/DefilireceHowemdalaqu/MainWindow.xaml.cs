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
using Windows.Win32.UI.Input.Pointer;
using Windows.Win32.UI.Input.Touch;

using Point = System.Drawing.Point;

namespace DefilireceHowemdalaqu;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _channel = Channel.CreateUnbounded<Action>();

        SourceInitialized += OnSourceInitialized;

        //StylusMove += MainWindow_StylusMove;
        //StylusUp += MainWindow_StylusUp;
        //TouchMove += MainWindow_TouchMove;
        //TouchUp += MainWindow_TouchUp;
        StylusPlugIns.Add(new F());

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

    private List<Point2D> _wpfPointList = [];
    private List<Point2D> _pointerPointList = [];

    private bool _isWpfUp;
    private bool _isPointerUp;

    private void MainWindow_TouchMove(object? sender, TouchEventArgs e)
    {
        var touchPoint = e.GetTouchPoint(RootGrid);
        var strokeVisual = GetStrokeVisual((uint) e.TouchDevice.Id);
        strokeVisual.Add(new StylusPoint(touchPoint.Position.X, touchPoint.Position.Y));
        strokeVisual.Redraw();
        Console.WriteLine($"WPF {e.TouchDevice.Id} XY={touchPoint.Position.X},{touchPoint.Position.Y}");

        if (!_isWpfUp)
        {
            _wpfPointList.Add(new Point2D(touchPoint.Position.X, touchPoint.Position.Y));
        }
    }

    private void MainWindow_TouchUp(object? sender, TouchEventArgs e)
    {
        StrokeVisualList.Remove((uint) e.TouchDevice.Id);
        _isWpfUp = true;
        Output();
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

        PInvoke.RegisterTouchWindow(new HWND(hwnd), REGISTER_TOUCH_WINDOW_FLAGS.TWF_WANTPALM);

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
    }

    private void AsyncConsole(string message) => _channel.Writer.TryWrite(() => Console.WriteLine(message));

    private unsafe IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int WM_POINTERDOWN = 0x0246;
        const int WM_POINTERUPDATE = 0x0245;
        const int WM_POINTERUP = 0x0247;

        if (msg is WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP)
        {
            var pointerId = (uint) (ToInt32(wparam) & 0xFFFF);
            PInvoke.GetPointerTouchInfo(pointerId, out var info);
            POINTER_INFO pointerInfo = info.pointerInfo;

            global::Windows.Win32.Foundation.RECT pointerDeviceRect = default;
            global::Windows.Win32.Foundation.RECT displayRect = default;

            PInvoke.GetPointerDeviceRects(pointerInfo.sourceDevice, &pointerDeviceRect, &displayRect);

            //Console.WriteLine($"PointerDeviceRect={pointerDeviceRect.X},{pointerDeviceRect.Y} WH={pointerDeviceRect.Width},{pointerDeviceRect.Height} DisplayRect={displayRect.X},{displayRect.Y} WH={displayRect.Width},{displayRect.Height}");

            //Console.WriteLine($"Id={pointerId}; PixelLocation={pointerInfo.ptPixelLocation.X},{pointerInfo.ptPixelLocation.Y}; Raw={pointerInfo.ptPixelLocationRaw.X},{pointerInfo.ptPixelLocationRaw.Y} Same={pointerInfo.ptPixelLocation== pointerInfo.ptPixelLocationRaw}");

            var point = pointerInfo.ptPixelLocation;
            PInvoke.ScreenToClient(new HWND(hwnd), ref point);

            AsyncConsole($"Pointer {pointerId} XY={point.X},{point.Y} Himetric={pointerInfo.ptHimetricLocationRaw.X},{pointerInfo.ptHimetricLocationRaw.Y}");
            return IntPtr.Zero;

            var point2D = new Point2D(point.X, point.Y);

            point2D = new Point2D(pointerInfo.ptHimetricLocationRaw.X / (double) pointerDeviceRect.Width * displayRect.Width + displayRect.left, pointerInfo.ptHimetricLocationRaw.Y / (double) pointerDeviceRect.Height * displayRect.Height + displayRect.top);
            //var p = PointFromScreen(new System.Windows.Point(point2D.X, point2D.Y));
            //point2D = new Point2D(p.X, p.Y);

            var screenTranslate = new Point(0, 0);
            PInvoke.ClientToScreen(new HWND(hwnd), ref screenTranslate);
            var dpi = VisualTreeHelper.GetDpi(this);
            point2D = new Point2D(point2D.X - screenTranslate.X, point2D.Y - screenTranslate.Y);
            point2D = new Point2D(point2D.X / dpi.DpiScaleX, point2D.Y / dpi.DpiScaleY);

            Console.WriteLine($"{point2D.X},{point2D.Y}");

            if (!_isPointerUp)
            {
                _pointerPointList.Add(point2D);
            }

            if (msg == WM_POINTERUP)
            {
                _isPointerUp = true;
                Output();
            }

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
            var touchInputCount = wparam.ToInt32();

            var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
            if (PInvoke.GetTouchInputInfo(new HTOUCHINPUT(lparam), (uint) touchInputCount, pTouchInputs, sizeof(TOUCHINPUT)))
            {
                for (var i = 0; i < touchInputCount; i++)
                {
                    var touchInput = pTouchInputs[i];
                    var point = new Point(touchInput.x / 100, touchInput.y / 100);
                    PInvoke.ScreenToClient(new HWND(hwnd), ref point);

                    AsyncConsole($"Touch {touchInput.dwID} XY={point.X}, {point.Y}");
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



    private void Output()
    {
        if (_isWpfUp && _isPointerUp)
        {
            Console.WriteLine($"WPF 触摸点数量： {_wpfPointList.Count} Pointer点数量： {_pointerPointList.Count}");

            for (int i = 0; i < _wpfPointList.Count || i < _pointerPointList.Count; i++)
            {
                string message;
                if (i < _wpfPointList.Count)
                {
                    message = $"{_wpfPointList[i].X:0000},{_wpfPointList[i].Y:0000}";
                }
                else
                {
                    message = "    ,    ";
                }

                message += " | ";
                if (i < _pointerPointList.Count)
                {
                    message += $"{_pointerPointList[i].X:0000},{_pointerPointList[i].Y:0000}";
                }

                Console.WriteLine(message);
            }
        }
    }

    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);

    private void CheckBox_OnClick(object sender, RoutedEventArgs e)
    {
        StrokeVisual.ShouldReCreatePoint = (sender as CheckBox)?.IsChecked ?? false;
    }
}

readonly record struct Point2D(double X, double Y);

class F : StylusPlugIn
{
    protected override void OnStylusMove(RawStylusInput rawStylusInput)
    {
        base.OnStylusMove(rawStylusInput);
    }
}