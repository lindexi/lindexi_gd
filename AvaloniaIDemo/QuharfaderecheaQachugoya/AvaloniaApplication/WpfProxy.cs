//using System.Threading;
//using System.Windows;
//using System.Windows.Threading;

//namespace AvaloniaApplication;

//class WpfProxy
//{
//    public WpfProxy()
//    {
//        var manualResetEventSlim = new ManualResetEventSlim(false);

//        Application application = null!;

//        var thread = new Thread(() =>
//        {
//            application = new Application();
//            // ReSharper disable once AccessToDisposedClosure
//            manualResetEventSlim.Set();
//            application.Run();
//        })
//        {
//            IsBackground = true,
//            Name = "WPF"
//        };

//        thread.SetApartmentState(ApartmentState.STA);

//        thread.Start();

//        manualResetEventSlim.Wait();
//        manualResetEventSlim.Dispose();

//        WpfApplication = application;
//    }

//    public Application WpfApplication { get; }

//    public void ShowWpfWindow(nint avaloniaWindowHandle)
//    {
//        WpfApplication.Dispatcher.InvokeAsync(() =>
//        {
//            var mainWindow = new WpfApplication.MainWindow();
//            mainWindow.Show();
//            mainWindow.SetTransparentHitThrough();
//            mainWindow.SetOwner(avaloniaWindowHandle);
//        },DispatcherPriority.Send);
//    }

//    public void MoveBorder(double x)
//    {
//        WpfApplication.Dispatcher.InvokeAsync(() =>
//        {
//            var mainWindow = (WpfApplication.MainWindow) WpfApplication.MainWindow!;
//            mainWindow.MoveBorder(x);
//        }, DispatcherPriority.Send);
//    }
//}