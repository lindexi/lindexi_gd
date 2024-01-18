using Uno.UI.Runtime.Skia.Wpf;

using WpfApp = System.Windows.Application;

namespace KefalurcilaybelJallbuderenajel.WPF;
public partial class App : WpfApp
{
    public App()
    {
        HackInitializer.Init();

        var host = new WpfHost(Dispatcher, () => new AppHead());
        host.Run();
    }
}
