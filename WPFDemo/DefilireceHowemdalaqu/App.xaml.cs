using System.Configuration;
using System.Data;
using System.Windows;

namespace DefilireceHowemdalaqu;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
    }
}

