using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RebeahairjairallWihemweechi;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        TestLeak();
    }

    private void TestLeak()
    {
        PresentationTraceSources.Refresh();
        var window = new Window();
        var wr = AddAndRemoveImage(window);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        //Assert.IsFalse(wr.IsAlive);
        //Debug.Assert(wr.IsAlive is false);
        Console.WriteLine($"{wr.IsAlive is false}");
    }

    static WeakReference AddAndRemoveImage(Window window)
    {
        var img = new Image();
        var wr = new WeakReference(img);
        var holder = new ImageHolder(img);
        window.Content = img;
        window.Show();
        DoEvents();
        window.Content = null;
        DoEvents();
        return wr;
    }

    static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.InvokeAsync(() => frame.Continue = false, DispatcherPriority.Background);
        Dispatcher.PushFrame(frame);
    }

    class ImageHolder
    {
        readonly Image image;
        public ImageHolder(Image image)
        {
            this.image = image;
            this.image.Loaded += OnLoaded;
            this.image.Unloaded += OnUnloaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OnLoaded");
        }

        void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OnUnloaded");
        }
    }
}

