using System.Configuration;
using System.Data;
using System.Windows;

namespace JurlerchalleecheQichewhayharnerede;

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
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var cts = new CancellationTokenSource();
        var task = Task.Factory.StartNew(() =>
        {
            while (true)
            {
                Console.WriteLine($"{Guid.NewGuid()}");
                try
                {
                    throw new Exception("aa");
                }
                catch (Exception ex)
                {
                    //Thread.Sleep(1000);
                }
            }
        }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}

