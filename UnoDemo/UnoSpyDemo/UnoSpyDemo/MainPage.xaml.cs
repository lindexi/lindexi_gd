using dotnetCampus.UISpy.Uno;

namespace UnoSpyDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        this.AttachDevTools();
    }
}
