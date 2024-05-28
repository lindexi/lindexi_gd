using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace RalllawfairlekolairHemyiqearkice;
internal class PlatformProvider:IPlatformProvider
{
    public PlatformProvider(Window window)
    {
        _window = window;
    }

    private readonly Window _window;

    public void EnterFullScreen()
    {
        _window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    public void ExitFullScreen()
    {
        _window.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
    }
}
