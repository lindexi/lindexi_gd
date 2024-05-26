using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FelawchechadaGeqedaihallnela;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        RoutedEventHandler handler = (_, _) =>
        {
            System.Diagnostics.Debug.WriteLine("PointerPressed");
        };

        AddHandler(PointerPressedEvent, handler, true);
    }
}
