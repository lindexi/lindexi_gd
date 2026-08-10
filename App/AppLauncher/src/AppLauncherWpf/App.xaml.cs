using System.ComponentModel;
using System.Windows;

namespace AppLauncherWpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            MainWindow = new MainWindow();
        }
        catch (Win32Exception)
        {
            MessageBox.Show(
                (string)FindResource("HotkeyRegistrationFailedText"),
                (string)FindResource("ErrorCaption"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
