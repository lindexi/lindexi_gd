using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.NativeUI;
using static Lsj.Util.Win32.Enums.ExtendedWindowStyles;
using static Lsj.Util.Win32.Enums.WindowStyles;
using static Lsj.Util.Win32.Constants;

namespace MinWidthSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var cxEdge = User32.GetSystemMetrics(SystemMetricIndexes.SM_CXEDGE);
            var cxSMSize = User32.GetSystemMetrics(SystemMetricIndexes.SM_CXSMSIZE);

            var uiThreadManager = new UIThreadManager();
            _ = uiThreadManager.InvokeAsync(() =>
            {
                var win = new Win32Window();
                win.Text = "MinWidthSampleWindow";
                win.Show();
                var rect = win.Rect;
                rect.right = rect.left + 1;
                win.Rect = rect;
                rect = win.Rect;
                Console.WriteLine(rect.right - rect.left); // 默认状态 136

                var style = win.WindowStyles;
                var styleEx = win.WindowStylesEx;

                win.WindowStylesEx |= WS_EX_TOOLWINDOW;
                rect = win.Rect;
                rect.right = rect.left + 1;
                win.Rect = rect;
                rect = win.Rect;
                Console.WriteLine(rect.right - rect.left); // WS_EX_TOOLWINDOW = 42

                win.WindowStyles &= ~WS_SYSMENU;
                rect = win.Rect;
                rect.right = rect.left + 1;
                win.Rect = rect;
                rect = win.Rect;
                Console.WriteLine(rect.right - rect.left); // WS_EX_TOOLWINDOW + 去掉 WS_SYSMENU =  20

                win.WindowStyles &= ~WS_DLGFRAME;
                win.WindowStyles &= ~WS_SIZEBOX;
                rect = win.Rect;
                rect.right = rect.left + 1;
                win.Rect = rect;
                rect = win.Rect;
                Console.WriteLine(rect.right - rect.left); // WS_EX_TOOLWINDOW + 去掉 WS_SYSMENU WS_DLGFRAME WS_SIZEBOX = 6

                // 这里实际的样式排列组合关系比较复杂
            });

            Console.ReadLine();
        }
    }
}
