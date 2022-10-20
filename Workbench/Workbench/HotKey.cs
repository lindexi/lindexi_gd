using System;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Workbench;

class HotKey
{
    public void Start()
    {
        KeyboardHookListener.KeyDown += KeyboardHookListener_KeyDown;

        KeyboardHookListener.HookKeyboard();
    }

    private void KeyboardHookListener_KeyDown(object? sender, KeyboardHookListener.RawKeyEventArgs args)
    {
        if (args.Key == Key.LeftCtrl)
        {
            var time = DateTime.Now;
            if (time - _lastCtrlKeyDown < TimeSpan.FromSeconds(1))
            {
                _lastCtrlKeyDown = DateTime.MinValue;

                MainWindow.ShowMainWindow();
            }
            else
            {
                _lastCtrlKeyDown = time;
            }
        }
    }

    private DateTime _lastCtrlKeyDown=DateTime.MinValue;
}