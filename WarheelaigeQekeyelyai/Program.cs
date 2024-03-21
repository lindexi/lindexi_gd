using Cairo;

using Gdk;

using Gtk;

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

    private void F_TouchEvent(object o, TouchEventArgs args)
    {
        //Console.WriteLine($"{args.Event.Type} {args.Event.Handle}");

        var eventTouch = EventTouch.New(args.Event.Handle);
        //Console.WriteLine($"EventTouch {eventTouch.X} {eventTouch.Y}");
        _point = (eventTouch.X, eventTouch.Y);
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