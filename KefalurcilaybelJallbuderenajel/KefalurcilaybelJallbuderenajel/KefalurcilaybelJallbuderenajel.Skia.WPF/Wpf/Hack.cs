using Microsoft.Maui.Graphics.UnoAbstract;
using Microsoft.UI.Xaml;

namespace KefalurcilaybelJallbuderenajel.WPF;

public class Hack : IHack
{
    public FrameworkElement Create()
    {
        return new HackElement();
    }
}
