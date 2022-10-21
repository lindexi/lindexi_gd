using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Workbench;

class HotKey
{
    public void Start()
    {
        KeyboardHookListener.KeyDown += KeyboardHookListener_KeyDown;
        KeyboardHookListener.KeyUp += KeyboardHookListener_KeyUp;

        KeyboardHookListener.HookKeyboard();
    }

    private void KeyboardHookListener_KeyUp(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LeftCtrl)
        {
            var time = DateTime.Now;
            if (time - _lastCtrlKeyDown < TimeSpan.FromMilliseconds(500))
            {
                _lastCtrlKeyDown = DateTime.MinValue;

                MainWindow.ShowMainWindow();
            }
            else
            {
                _lastCtrlKeyDown = time;
            }
        }

        //Debug.WriteLine($"KeyUp {args.Key}");
    }

    private void KeyboardHookListener_KeyDown(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
       
    }

    private DateTime _lastCtrlKeyDown=DateTime.MinValue;
}