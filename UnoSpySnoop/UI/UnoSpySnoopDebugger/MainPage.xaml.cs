using dotnetCampus.Ipc.IpcRouteds.DirectRouteds;

using UnoSpySnoopDebugger.View;

namespace UnoSpySnoopDebugger;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        Loaded += MainPage_Loaded;

        var ipcProvider = new JsonIpcDirectRoutedProvider();
        IpcProvider = ipcProvider;
    }

    public JsonIpcDirectRoutedProvider IpcProvider { get; set; }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        var debugName = "UnoSpySnoop"; // todo Update to selected name
        JsonIpcDirectRoutedClientProxy client = await IpcProvider.GetAndConnectClientAsync(debugName);
        var snoopUserControl = new SnoopUserControl(client);
        await snoopUserControl.StartAsync();

        RootGrid.Children.Add(snoopUserControl);
    }
}
