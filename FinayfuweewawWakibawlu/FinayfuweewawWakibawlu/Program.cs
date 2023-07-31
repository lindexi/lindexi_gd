

using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.ViewManagement;

namespace FinayfuweewawWakibawlu;

internal class Program
{
    [STAThread]
    unsafe static void Main(string[] args)
    {
        var mainView = CoreApplication.MainView;

        var forCurrentThread = DispatcherQueue.GetForCurrentThread();

        var coreApplicationView = CoreApplication.CreateNewView();


        var applicationView = new ABI.Windows.UI.ViewManagement.ApplicationView();
        var p = &applicationView;

        var fromAbi = ApplicationView.FromAbi(new IntPtr(p));

        var currentView = ApplicationView.GetForCurrentView();


        Console.WriteLine("Hello, World!");
    }
}
