namespace SplashScreensLib.SplashScreens;

/// <summary>
/// 欢迎界面显示了的事件参数
/// </summary>
/// <param name="windowHandler"></param>
public class SplashScreenShowedEventArgs(IntPtr windowHandler) : EventArgs
{
    public IntPtr SplashScreenWindowHandler => windowHandler;
}