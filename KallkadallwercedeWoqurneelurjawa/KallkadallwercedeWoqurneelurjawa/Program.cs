// See https://aka.ms/new-console-template for more information

using Gtk;
namespace KallkadallwercedeWoqurneelurjawa;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.Init();

        var app = new Application("com.companyname.skiasharpsample", GLib.ApplicationFlags.None);
        app.Register(GLib.Cancellable.Current);

        var win = new MainWindow();
        app.AddWindow(win);

        win.Show();
        Application.Run();
    }
}
