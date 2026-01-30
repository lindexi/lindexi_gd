using System.Runtime.InteropServices;

using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

using static Windows.Win32.PInvoke;

namespace AngleOpenGLDemo;

public class AngleOpenGLApplication
{
    public void ShowMainWindow(nint ownerWindowHandle)
    {

    }


    public void MoveBorder(double x)
    {
        _currentPosition = _currentPosition with
        {
            X = x
        };
    }

    private volatile Position _currentPosition = new(0, 600);

    record Position(double X, double Y)
    {
        // 为什么需要 class 引用类型，而不是值类型？为了不加锁跨线程访问
    }

    private unsafe HWND CreateWindow()
    {
        DwmIsCompositionEnabled(out var compositionEnabled);

        if (!compositionEnabled)
        {
            Console.WriteLine($"无法启用透明窗口效果");
        }

        // 要求 Layered 窗口仅仅是为了显示透明窗口，详细请参阅 [Vortice 使用 DirectComposition 显示透明窗口 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/19541356 )
        WINDOW_EX_STYLE exStyle = WINDOW_EX_STYLE.WS_EX_OVERLAPPEDWINDOW
                                  | WINDOW_EX_STYLE.WS_EX_LAYERED; // Layered 是透明窗口的最关键

        // 如果你想做无边框：
        //exStyle |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW; // 可选
        //exStyle |= WINDOW_EX_STYLE.WS_EX_TRANSPARENT; // 点击穿透可选

        var style = WNDCLASS_STYLES.CS_OWNDC | WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW;

        var defaultCursor = LoadCursor(
            new HINSTANCE(IntPtr.Zero), new PCWSTR(IDC_ARROW.Value));

        var className = $"lindexi-{Guid.NewGuid().ToString()}";
        var title = "The Title";
        fixed (char* pClassName = className)
        fixed (char* pTitle = title)
        {
            var wndClassEx = new WNDCLASSEXW
            {
                cbSize = (uint) Marshal.SizeOf<WNDCLASSEXW>(),
                style = style,
                lpfnWndProc = new WNDPROC(WndProc),
                hInstance = new HINSTANCE(GetModuleHandle(null).DangerousGetHandle()),
                hCursor = defaultCursor,
                hbrBackground = new HBRUSH(IntPtr.Zero),
                lpszClassName = new PCWSTR(pClassName)
            };
            ushort atom = RegisterClassEx(in wndClassEx);

            var dwStyle = WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
            // 去掉最大化按钮和可调边框
            //dwStyle &= ~(WINDOW_STYLE.WS_MAXIMIZEBOX | WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_CAPTION);
            // 保留最小化按钮
            //dwStyle |= WINDOW_STYLE.WS_MINIMIZEBOX;

            var windowHwnd = CreateWindowEx(
                exStyle,
                new PCWSTR((char*) atom),
                new PCWSTR(pTitle),
                dwStyle,
                0, 0, 1900, 1000,
                HWND.Null, HMENU.Null, HINSTANCE.Null, null);

            return windowHwnd;
        }
    }

    private LRESULT WndProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
    {
        switch ((WindowsMessage) message)
        {
            case WindowsMessage.WM_NCCALCSIZE:
                {
                    return new LRESULT(0);
                }
            //case WindowsMessage.WM_SIZE:
            //    {
            //        _renderManager?.ReSize();
            //        break;
            //    }
        }

        return DefWindowProc(hwnd, message, wParam, lParam);
    }
}