using System.Configuration;
using System.Data;
using System.Windows;

namespace WhalqojerwiKuwelbaiko;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        this.SessionEnding += App_SessionEnding;
    }

    private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        e.Cancel = true;
    }
}

