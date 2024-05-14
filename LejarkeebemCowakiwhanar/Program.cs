using Gdk;
using Gtk;
using Window = Gtk.Window;
using WindowType = Gtk.WindowType;

namespace LejarkeebemCowakiwhanar;

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

        Title = title;
    }

    protected override void OnShown()
    {
        Console.WriteLine("OnShown");
        base.OnShown();
    }

    protected override void OnActivate()
    {
        Console.WriteLine("OnActivate");
        base.OnActivate();
    }
}
