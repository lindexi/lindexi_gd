using Gdk;
using Gtk;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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

        _win = new MainWindow("lindexi shi doubi");
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
        base.OnShown(); // 在这句话调用之前 window.Window 是空

        Window window = this;
        Gdk.Window gdkWindow = window.Window;

        if (gdkWindow is null)
        {
            // 确保 base.OnShown 调用
            Console.WriteLine($"gdkWindow is null");
            return;
        }

        var x11 = gdk_x11_window_get_xid(gdkWindow.Handle);
        Console.WriteLine($"X11 窗口 0x{x11:x2}");
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr d_gdk_x11_window_get_xid(IntPtr gdkWindow);

    private static d_gdk_x11_window_get_xid gdk_x11_window_get_xid =
        LoadFunction<d_gdk_x11_window_get_xid>("libgdk-3.so.0", "gdk_x11_window_get_xid");

    private static T LoadFunction<T>(string libName, string functionName)
    {
        if (!OperatingSystem.IsLinux())
        {
            // 防止炸调试
            return default(T)!;
        }

        // LoadLibrary
        var libPtr = Linux.dlopen(libName, RTLD_GLOBAL | RTLD_LAZY);

        Console.WriteLine($"Load {libName} ptr={libPtr}");

        // GetProcAddress
        var procAddress = Linux.dlsym(libPtr, functionName);
        Console.WriteLine($"Load {functionName} ptr={procAddress}");
        return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
    }

    private const int RTLD_LAZY = 0x0001;
    private const int RTLD_GLOBAL = 0x0100;

    private class Linux
    {
        [DllImport("libdl.so.2")]
        public static extern IntPtr dlopen(string path, int flags);

        [DllImport("libdl.so.2")]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);
    }
}
