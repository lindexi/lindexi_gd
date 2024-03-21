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

        App = new Application("org.Samples.Samples", GLib.ApplicationFlags.None);
        App.Register(GLib.Cancellable.Current);

        Win = new MainWindow("Demo Window");
        App.AddWindow(Win);

        Win.ShowAll();
        Application.Run();
    }

    public static Application App;
    public static Window Win;
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
        TouchEvent += F_TouchEvent;
    }

    private unsafe void F_TouchEvent(object o, TouchEventArgs args)
    {
        //Console.WriteLine($"{args.Event.Type} {args.Event.Handle}");

        var eventTouch = EventTouch.New(args.Event.Handle);
        //Console.WriteLine($"EventTouch {eventTouch.X} {eventTouch.Y}");
        _point = (eventTouch.X, eventTouch.Y);

        if (eventTouch.Type == EventType.TouchBegin)
        {
            var device = eventTouch.Device;
            var numAxes = device.NumAxes;

            Console.WriteLine($"NumAxes={numAxes}");

            var span = new Span<double>((void*) eventTouch.Axes, numAxes);
            var axes = span.ToArray();

            if (device.GetAxis(axes, AxisUse.X, out var value))
            {
                Console.WriteLine($"AxisUse.X={value}");
            }

            if (device.GetAxis(axes, AxisUse.Y, out value))
            {
                Console.WriteLine($"AxisUse.Y={value}");
            }

            if (device.GetAxis(axes, AxisUse.Pressure, out value))
            {
                Console.WriteLine($"Pressure={value}");
            }

            if (device.GetAxis(axes, AxisUse.Xtilt, out value))
            {
                Console.WriteLine($"Xtilt={value}");
            }

            if (device.GetAxis(axes, AxisUse.Ytilt, out value))
            {
                Console.WriteLine($"Ytilt={value}");
            }

            if (device.GetAxis(axes, AxisUse.Wheel, out value))
            {
                Console.WriteLine($"Wheel={value}");
            }

            if (device.GetAxis(axes, AxisUse.Distance, out value))
            {
                Console.WriteLine($"Distance={value}");
            }

            if (device.GetAxis(axes, AxisUse.Rotation, out value))
            {
                Console.WriteLine($"Rotation={value}");
            }

            if (device.GetAxis(axes, AxisUse.Slider, out value))
            {
                Console.WriteLine($"Slider={value}");
            }

            for (int i = 10; i < numAxes; i++)
            {
                
            }

            /*
               Xtilt,
               Ytilt,
               Wheel,
               Distance,
               Rotation,
               Slider,
             */
        }

        QueueDraw();
    }

    private (double X, double Y) _point = (100, 100);

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
    }

    protected override bool OnDrawn(Context cr)
    {
        cr.SetSourceRGB(0.9, 0, 0);
        cr.LineWidth = 10;
        //cr.MoveTo(10, 10);
        //cr.LineTo(_point.X, _point.Y);
        var size = 10d;
        cr.Rectangle(_point.X - size / 2, _point.Y - size / 2, size, size);
        cr.Stroke();
        return base.OnDrawn(cr);
    }
}

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
    public Gdk.EventSequence Sequence
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