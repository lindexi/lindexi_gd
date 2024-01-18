using Microsoft.UI.Xaml;
using SamplesApp;

namespace KefalurcilaybelJallbuderenajel.WPF;

public class Hack : IHack
{
    public FrameworkElement Create()
    {
        return new GraphicsCanvasElement()
        {
            Width = 200,
            Height = 100
        };
    }
}
