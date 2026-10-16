using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.NativeUI;

namespace SimpleWin32Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var uiThreadManager = new UIThreadManager();
            _ = uiThreadManager.InvokeAsync(() =>
            {
                var win = new TestWin32Window();
                win.Show();
                //unsafe
                //{
                //    var b = (BOOL)false;
                //    var result = false;
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_TOUCH_CONTACTVISUALIZATION, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_PEN_BARRELVISUALIZATION, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_PEN_TAP, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_PEN_DOUBLETAP, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_PEN_PRESSANDHOLD, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_PEN_RIGHTTAP, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_TOUCH_TAP, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_TOUCH_DOUBLETAP, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_TOUCH_PRESSANDHOLD, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_TOUCH_RIGHTTAP, 0, (uint)sizeof(BOOL), &b);
                //    result = User32.SetWindowFeedbackSetting(win.Handle, FEEDBACK_TYPE.FEEDBACK_GESTURE_PRESSANDTAP, 0, (uint)sizeof(BOOL), &b);
                //}
                var win2 = new Win32Window();
                win2.Show();
            });

            Console.ReadLine();
        }
    }

    class TestWin32Window : Win32Window
    {
        protected override LRESULT WindowProc(HWND hWnd, WindowMessages msg, WPARAM wParam, LPARAM lParam)
        {
            //Console.WriteLine($"[WindowProc] {msg}");
            return base.WindowProc(hWnd, msg, wParam, lParam);
        }

        //protected override LRESULT OnActivate(HWND hWnd, WPARAM wParam, LPARAM lParam, WindowActivateTypes windowActivateTypes, bool isMinimized, HWND activateOrDeactivateWindowHandle)
        //{
        //    Console.WriteLine($"[OnActivate] Type: {windowActivateTypes}, IsMinimized: {isMinimized}, Handle: {activateOrDeactivateWindowHandle}");
        //    return base.OnActivate(hWnd, wParam, lParam, windowActivateTypes, isMinimized, activateOrDeactivateWindowHandle);
        //}

        protected override LRESULT OnSetFocus(HWND hWnd, WPARAM wParam, LPARAM lParam, HWND lostFocusWindowHandle)
        {
            Console.WriteLine($"[OnSetFocus] Handle: {lostFocusWindowHandle}");
            return base.OnSetFocus(hWnd, wParam, lParam, lostFocusWindowHandle);
        }

        protected override LRESULT OnKillFocus(HWND hWnd, WPARAM wParam, LPARAM lParam, HWND receiveFocusWindowHandle)
        {
            Console.WriteLine($"[OnKillFocus] Handle: {receiveFocusWindowHandle}");
            return base.OnSetFocus(hWnd, wParam, lParam, receiveFocusWindowHandle);
        }
    }
}
