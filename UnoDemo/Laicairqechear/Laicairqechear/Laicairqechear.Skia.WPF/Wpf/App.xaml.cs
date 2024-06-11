using Uno.UI.Runtime.Skia.Wpf;

using WpfApp = System.Windows.Application;

namespace Laicairqechear.WPF;
public partial class App : WpfApp
{
    public App()
    {
        var host = new WpfHost(Dispatcher, () => new AppHead());
        host.Run();
    }
}
