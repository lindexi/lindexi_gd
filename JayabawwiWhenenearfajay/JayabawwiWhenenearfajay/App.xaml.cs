using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace JayabawwiWhenenearfajay;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static Task Task { private set; get; }

    public static ConcurrentQueue<ResourceDictionary> Queue { get; } = new ConcurrentQueue<ResourceDictionary>();

    protected override void OnStartup(StartupEventArgs e)
    {
        Task = Task.Run(async () =>
        {
            bool foo = true;
            while (foo)
            {
                await Task.Delay(1000);
            }

            var resourceDictionary = new ResourceDictionary()
            {
                Source = new Uri("/Dictionary1.xaml",
                    UriKind.RelativeOrAbsolute)
            };

            Queue.Enqueue(resourceDictionary);
        });

        base.OnStartup(e);
    }
}