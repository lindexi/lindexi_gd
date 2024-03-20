using Cairo;
using Gdk;
using Gtk;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace WarheelaigeQekeyelyai;

internal class Program
{
    static void Main(string[] args)
    {
        Application.Init();

        App = new Application("org.Samples.Samples", GLib.ApplicationFlags.None);
        App.Register(GLib.Cancellable.Current);

        Win = new MainWindow("Demo Window");
        App.AddWindow(Win);
        Win.Show();
        Application.Run();
    }

    public static Application App;
    public static Window Win;
}

class MainWindow : Window
{
    public MainWindow(string title) : base(title)
    {
        WindowPosition = WindowPosition.Center;
        DefaultSize = new Size(600, 600);

        Widget widget = this;
        this.TouchEvent += MainWindow_TouchEvent;

        Child = new Box(Orientation.Horizontal, 0)
        {
            WidthRequest = 500,
            HeightRequest = 500,
            Child = new F()
        };
    }

    private void MainWindow_TouchEvent(object o, TouchEventArgs args)
    {
    }

    protected override bool OnDrawn(Context cr)
    {
        //Console.WriteLine(cr.Status);
        //if (cr.GetSource() is SurfacePattern surfacePattern)
        //{
        //}
        cr.SetSourceRGB(0.9,0,0);
        cr.LineWidth = 10;
        cr.MoveTo(10,10);
        cr.LineTo(100,10);
        cr.Stroke();
        
        var result = base.OnDrawn(cr);
        return true;
    }
}

class F : Widget
{
    public F()
    {
        WidthRequest = 300;
        HeightRequest = 300;
        
    }

    protected override bool OnDrawn(Context cr)
    {
        cr.SetSourceRGB(0.9, 0, 0);
        cr.LineWidth = 10;
        cr.MoveTo(10, 10);
        cr.LineTo(100, 10);
        cr.Stroke();
        return base.OnDrawn(cr);
    }
}