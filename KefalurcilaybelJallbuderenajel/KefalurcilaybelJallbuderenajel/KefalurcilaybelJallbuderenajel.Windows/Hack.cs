using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.UnoAbstract;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KefalurcilaybelJallbuderenajel.Hacking;

class Hack : IHack
{
    public FrameworkElement Create()
    {
        return new HackElement();
    }
}


class HackElement : ContentControl, IDrawableNotify
{
    public HackElement()
    {
        this.Loaded += HackElement_Loaded;
    }

    private void HackElement_Loaded(object sender, RoutedEventArgs e)
    {
      
    }

    public event EventHandler<ICanvas>? Draw;
}
