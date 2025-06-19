namespace DotNetCampus.Installer.Lib.SplashScreens;

public class SplashScreenShowedEventArgs(IntPtr windowHandler) : EventArgs
{
    public IntPtr SplashScreenWindowHandler => windowHandler;
}