using System.Configuration;
using System.Data;
using System.Windows;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;

namespace JoquryurjoqaJelhawlujel;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppCenter.Start("{Your App Secret}", typeof(App), typeof(Crashes));
    }
}

