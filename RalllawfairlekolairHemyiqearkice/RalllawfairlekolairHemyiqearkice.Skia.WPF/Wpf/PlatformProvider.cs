using System.Windows;
using RalllawfairlekolairHemyiqearkice.Skia.Wpf.Utils;

namespace RalllawfairlekolairHemyiqearkice;

public class PlatformProvider : IPlatformProvider
{
    public PlatformProvider(Window window)
    {
        _window = window;
    }

    private readonly Window _window;

    public void EnterFullScreen()
    {
        FullScreenHelper.StartFullScreen(_window);
    }

    public void ExitFullScreen()
    {
        FullScreenHelper.EndFullScreen(_window);
    }
}
