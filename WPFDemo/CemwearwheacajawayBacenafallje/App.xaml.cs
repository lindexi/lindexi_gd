using System.Configuration;
using System.Data;
using System.Windows;

namespace CemwearwheacajawayBacenafallje;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
    }

    private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        FirstException = e.Exception;
    }

    public Exception? FirstException { get; set; }
}

