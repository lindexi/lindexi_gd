using Microsoft.Maui.Graphics.UnoAbstract;
using Microsoft.UI.Xaml;

namespace KefalurcilaybelJallbuderenajel.Skia.Gtk;

public class Hack : IHack
{
    public FrameworkElement Create()
    {
        return new HackElement();
    }
}
