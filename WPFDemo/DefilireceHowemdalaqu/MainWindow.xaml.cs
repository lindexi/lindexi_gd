using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
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

        SourceInitialized += OnSourceInitialized;

        //StylusMove += MainWindow_StylusMove;
        //StylusUp += MainWindow_StylusUp;
        TouchMove += MainWindow_TouchMove;
        TouchUp += MainWindow_TouchUp;
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
        var visualCanvas = new VisualCanvas(strokeVisual);
        RootGrid.Children.Add(visualCanvas);

        return strokeVisual;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        PInvoke.RegisterTouchWindow(new HWND(hwnd), 0);

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
    }

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

            //Console.WriteLine($"Id={pointerId}; PixelLocation={pointerInfo.ptPixelLocation.X},{pointerInfo.ptPixelLocation.Y}; Raw={pointerInfo.ptPixelLocationRaw.X},{pointerInfo.ptPixelLocationRaw.Y} Same={pointerInfo.ptPixelLocation== pointerInfo.ptPixelLocationRaw}");

            var point = pointerInfo.ptPixelLocation;
            PInvoke.ScreenToClient(new HWND(hwnd), ref point);

            Console.WriteLine($"Pointer {pointerId} XY={point.X},{point.Y}");

            if (!_isPointerUp)
            {
                _pointerPointList.Add(new Point2D(point.X, point.Y));
            }

            if (msg == WM_POINTERUP)
            {
                _isPointerUp = true;
                Output();
            }

            //if (msg == WM_POINTERUPDATE)
            //{
            //    var strokeVisual = GetStrokeVisual(pointerId);
            //    strokeVisual.Add(new StylusPoint(point.X, point.Y));
            //    strokeVisual.Redraw();
            //}
            //else if (msg == WM_POINTERUP)
            //{
            //    StrokeVisualList.Remove(pointerId);
            //}
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

                    Console.WriteLine($"Touch {touchInput.dwID} XY={point.X}, {point.Y}");

                    if (touchInput.dwFlags.HasFlag(TOUCHEVENTF_FLAGS.TOUCHEVENTF_MOVE))
                    {
                        var strokeVisual = GetStrokeVisual(touchInput.dwID);
                        strokeVisual.Add(new StylusPoint(point.X, point.Y));
                        strokeVisual.Redraw();
                    }
                    else if (touchInput.dwFlags.HasFlag(TOUCHEVENTF_FLAGS.TOUCHEVENTF_UP))
                    {
                        StrokeVisualList.Remove(touchInput.dwID);
                    }
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
                if(i< _pointerPointList.Count)
                {
                    message += $"{_pointerPointList[i].X:0000},{_pointerPointList[i].Y:0000}";
                }

                Console.WriteLine(message);
            }
        }
    }

    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int) (ptr.ToInt64() & 0xffffffff);

    /// <summary>
    ///     用于显示笔迹的类
    /// </summary>
    public class StrokeVisual : DrawingVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        public StrokeVisual() : this(new DrawingAttributes()
        {
            Color = Colors.Red,
            FitToCurve = true,
            Width = 5
        })
        {
        }

        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes;
        }

        private readonly DrawingAttributes _drawingAttributes;

        /// <summary>
        ///     设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { set; get; }

        /// <summary>
        ///     在笔迹中添加点
        /// </summary>
        /// <param name="point"></param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                var collection = new StylusPointCollection { point };
                Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        ///     重新画出笔迹
        /// </summary>
        public void Redraw()
        {
            using var dc = RenderOpen();
            Stroke.Draw(dc);
        }
    }

    public class VisualCanvas : FrameworkElement
    {
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        protected override int VisualChildrenCount => 1;

        public VisualCanvas(DrawingVisual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
        }

        public DrawingVisual Visual { get; }
    }
}

readonly record struct Point2D(double X, double Y);