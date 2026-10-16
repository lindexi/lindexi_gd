using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;
using WindowSamples;

using var window = new NativeWindow("MinWidthSampleWindow");
window.Show();
var style = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
int normal = window.ResizeToMinimum();
window.SetStyles(style, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
int tool = window.ResizeToMinimum();
style &= ~WINDOW_STYLE.WS_SYSMENU;
window.SetStyles(style, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
int noMenu = window.ResizeToMinimum();
style &= ~(WINDOW_STYLE.WS_DLGFRAME | WINDOW_STYLE.WS_SIZEBOX);
window.SetStyles(style, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
int border = window.ResizeToMinimum();
if (args.Contains("--self-test"))
{
    if (normal < tool || tool < noMenu || noMenu < border)
        throw new InvalidOperationException("Minimum width ordering failed.");
    Console.WriteLine($"PASS minimum widths, architecture={RuntimeInformation.ProcessArchitecture}");
}
else NativeWindow.RunMessageLoop();
