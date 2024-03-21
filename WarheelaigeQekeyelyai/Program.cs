using Atk;

using Cairo;

using Gdk;

using Gtk;

using System.Runtime.InteropServices;

using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace WarheelaigeQekeyelyai;

internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.Init();

        _app = new Application("org.Samples.Samples", GLib.ApplicationFlags.None);
        _app.Register(GLib.Cancellable.Current);

        _win = new MainWindow("Demo Window");
        _app.AddWindow(_win);

        _win.ShowAll();
        Application.Run();
    }

    private static Application? _app;
    private static Window? _win;
}

class MainWindow : Window
{
    public MainWindow(string title) : base(WindowType.Toplevel)
    {
        WindowPosition = WindowPosition.Center;
        DefaultSize = new Size(600, 600);

        Child = new F();
    }

    private void MainWindow_TouchEvent(object o, TouchEventArgs args)
    {
    }
}

class F : DrawingArea
{
    public F()
    {
        WidthRequest = 300;
        HeightRequest = 300;

        AddEvents((int) RequestedEvents);
        EnterNotifyEvent += F_EnterNotifyEvent;
        ButtonPressEvent += F_ButtonPressEvent;
        MotionNotifyEvent += F_MotionNotifyEvent;
        TouchEvent += F_TouchEvent;
    }


    private unsafe void F_TouchEvent(object o, TouchEventArgs args)
    {
        var eventTouch = EventTouch.New(args.Event.Handle);

        //if (eventTouch.Type == EventType.TouchBegin)
        {
            var device = eventTouch.Device;
            var numAxes = device.NumAxes;

            Console.WriteLine($"NumAxes={numAxes} Id={eventTouch.Sequence?.Handle ?? -1}");

            var axes = new Span<double>((void*) eventTouch.Axes, numAxes);
            //var axes = span.ToArray();

            // 删除没有用的代码，这些代码无法获取值
            //if (device.GetAxis(axes, AxisUse.X, out var value))
            //{
            //    Console.WriteLine($"AxisUse.X={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Y, out value))
            //{
            //    Console.WriteLine($"AxisUse.Y={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Pressure, out value))
            //{
            //    Console.WriteLine($"Pressure={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Xtilt, out value))
            //{
            //    Console.WriteLine($"Xtilt={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Ytilt, out value))
            //{
            //    Console.WriteLine($"Ytilt={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Wheel, out value))
            //{
            //    Console.WriteLine($"Wheel={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Distance, out value))
            //{
            //    Console.WriteLine($"Distance={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Rotation, out value))
            //{
            //    Console.WriteLine($"Rotation={value}");
            //}

            //if (device.GetAxis(axes, AxisUse.Slider, out value))
            //{
            //    Console.WriteLine($"Slider={value}");
            //}

            Console.WriteLine("=================");
            for (int i = 0; i < numAxes; i++)
            {
                Console.WriteLine($"[{i}] {axes[i]}");
            }

            if (numAxes > 5)
            {
                var radioX = eventTouch.XRoot / axes[0];
                var radioY = eventTouch.YRoot / axes[1];

                var rawWidth = axes[3];
                var rawHeight = axes[4];

                var width = rawWidth * radioX;
                var height = rawHeight * radioY;

                Console.WriteLine($"Width={width} Height={height}");
            }

            Console.WriteLine("=================");
            Console.WriteLine();
        }

        if (eventTouch.Type == EventType.TouchBegin)
        {
            PointList.Clear();
        }

        PointList.Add(new(eventTouch.X, eventTouch.Y));

        QueueDraw();
    }

    private List<Point2D> PointList { get; } = new List<Point2D>();

    internal const Gdk.EventMask RequestedEvents =
        Gdk.EventMask.EnterNotifyMask
        | Gdk.EventMask.LeaveNotifyMask
        | Gdk.EventMask.ButtonPressMask
        | Gdk.EventMask.ButtonReleaseMask
        | Gdk.EventMask.PointerMotionMask // Move
        | Gdk.EventMask.SmoothScrollMask
        | Gdk.EventMask.TouchMask // Touch
        | Gdk.EventMask.ProximityInMask // Pen
        | Gdk.EventMask.ProximityOutMask // Pen
        | Gdk.EventMask.KeyPressMask
        | Gdk.EventMask.KeyReleaseMask;

    private void F_EnterNotifyEvent(object o, EnterNotifyEventArgs args)
    {
        PointList.Clear();
        PointList.Add(new Point2D(args.Event.X, args.Event.Y));
        QueueDraw();
    }

    private void F_ButtonPressEvent(object o, ButtonPressEventArgs args)
    {
        PointList.Clear();
        PointList.Add(new Point2D(args.Event.X, args.Event.Y));
        QueueDraw();
    }

    private void F_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
    {
        PointList.Add(new Point2D(args.Event.X, args.Event.Y));
        QueueDraw();
    }

    protected override bool OnDrawn(Context cr)
    {
        cr.SetSourceRGB(0.9, 0, 0);
        cr.LineWidth = 5;
        //cr.MoveTo(10, 10);
        //cr.LineTo(_point.X, _point.Y);
        //var size = 10d;
        //cr.Rectangle(_point.X - size / 2, _point.Y - size / 2, size, size);
        if (PointList.Count > 0)
        {
            var (x, y) = PointList[0];
            cr.MoveTo(x, y);
        }

        for (int i = 1; i < PointList.Count; i++)
        {
            var (x, y) = PointList[i];
            cr.LineTo(x, y);
        }

        cr.Stroke();
        return base.OnDrawn(cr);
    }
}

readonly record struct Point2D(double X, double Y);

[StructLayout(LayoutKind.Sequential)]
public partial struct EventTouch
{
    public Gdk.EventType Type;
    private IntPtr _window;
    public Gdk.Window Window
    {
        get
        {
            return GLib.Object.GetObject(_window) as Gdk.Window;
        }
        set
        {
            _window = value == null ? IntPtr.Zero : value.Handle;
        }
    }
    public sbyte SendEvent;
    public uint Time;
    public double X;
    public double Y;
    public IntPtr Axes;
    public uint State;
    private IntPtr _sequence;
    public Gdk.EventSequence? Sequence
    {
        get
        {
            return _sequence == IntPtr.Zero ? null : (Gdk.EventSequence) GLib.Opaque.GetOpaque(_sequence, typeof(Gdk.EventSequence), false);
        }
        set
        {
            _sequence = value == null ? IntPtr.Zero : value.Handle;
        }
    }
    public bool EmulatingPointer;
    private IntPtr _device;
    public Gdk.Device Device
    {
        get
        {
            return GLib.Object.GetObject(_device) as Gdk.Device;
        }
        set
        {
            _device = value == null ? IntPtr.Zero : value.Handle;
        }
    }
    public double XRoot;
    public double YRoot;

    public static EventTouch Zero = new EventTouch();

    public static EventTouch New(IntPtr raw)
    {
        if (raw == IntPtr.Zero)
            return EventTouch.Zero;
        return (EventTouch) Marshal.PtrToStructure(raw, typeof(EventTouch));
    }

    private static GLib.GType GType
    {
        get { return GLib.GType.Pointer; }
    }
}