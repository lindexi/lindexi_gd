using Uno.UI.Extensions;

namespace KefalurcilaybelJallbuderenajel;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        StackPanel.Children.Add(HackHelper.Hack.Create());
    }
}
