using System.Threading;
using System.Windows;
using AngleOpenGLDemo;

namespace AvaloniaApplication;

/// <summary>
///  AngleOpenGlApplication
/// </summary>
class AngleOpenGLApplicationProxy
{
    public AngleOpenGLApplicationProxy()
    {
    }

    private readonly AngleOpenGLApplication _angleOpenGLApplication = new ();

    public void ShowAngleOpenGLWindow(nint avaloniaWindowHandle)
    {
        _angleOpenGLApplication.ShowMainWindow(avaloniaWindowHandle);
    }

    public void MoveBorder(double x)
    {
        _angleOpenGLApplication.MoveBorder(x);
    }
}