namespace SpySnoopDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        UnoSpySnoop.SpySnoop.StartSpyUI(SnoopRootGrid);
    }
}
