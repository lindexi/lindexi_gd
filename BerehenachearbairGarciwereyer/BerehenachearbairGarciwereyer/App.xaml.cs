using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BerehenachearbairGarciwereyer;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        RunNewUIThread();
    }

    public static void RunNewUIThread()
    {
        Thread thread = new(Run);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        void Run()
        {
            var currentDispatcher =
            System.Windows.Threading.Dispatcher.CurrentDispatcher;
            currentDispatcher.InvokeAsync(() =>
            {
                //_event1.Set();
                //_event2.WaitOne();

                TouchContentPresenter();
            });
            System.Windows.Threading.Dispatcher.Run();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TouchContentPresenter()
    {
        // Just call the .cctor in ContentPresenter.
        var property = ContentPresenter.ContentProperty;
        CaptureObject(property);
    }

    private static void CaptureObject(object obj)
    {
        Debug.WriteLine(obj);
    }
}

