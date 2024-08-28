using System.Diagnostics;
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

    private void OnSourceInitialized(object sender, EventArgs e)
    {
        var windowInteropHelper = new WindowInteropHelper(this);
        var hwnd = windowInteropHelper.Handle;

        HwndSource source = HwndSource.FromHwnd(hwnd)!;
        source.AddHook(Hook);
    }

    private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        const int WM_POINTERDOWN = 0x0246;
        const int WM_POINTERUPDATE = 0x0245;
        const int WM_POINTERUP = 0x0247;

        if (msg is WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP)
        {
            var pointerId = (uint)(ToInt32(wparam) & 0xFFFF);
            PInvoke.GetPointerTouchInfo(pointerId, out var info);
            POINTER_INFO pointerInfo = info.pointerInfo;

            Debug.WriteLine($"PixelLocation={pointerInfo.ptPixelLocation.X},{pointerInfo.ptPixelLocation.Y}; Raw={pointerInfo.ptPixelLocationRaw.X},{pointerInfo.ptPixelLocationRaw.Y} Same={pointerInfo.ptPixelLocation== pointerInfo.ptPixelLocationRaw}");

            var point = pointerInfo.ptPixelLocation;
            PInvoke.ScreenToClient(new HWND(hwnd), ref point);

            if (msg == WM_POINTERUPDATE)
            {
                var strokeVisual = GetStrokeVisual(pointerId);
                strokeVisual.Add(new StylusPoint(point.X, point.Y));
                strokeVisual.Redraw();
            }
            else if (msg == WM_POINTERUP)
            {
                StrokeVisualList.Remove(pointerId);
            }
        }

        return IntPtr.Zero;
    }

    private static int ToInt32(IntPtr ptr) => IntPtr.Size == 4 ? ptr.ToInt32() : (int)(ptr.ToInt64() & 0xffffffff);

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