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

        Win.Show();
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
        Show();
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