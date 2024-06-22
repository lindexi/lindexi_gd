namespace UnoDemo;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
    
    private void RunAvaloniaButton_OnClick(object sender, RoutedEventArgs e)
    {
        //// Create the new Thread to run the Avalonia 
        
        //var thread = new Thread(() =>
        //{
        //    AvaloniaIDemo.Program.Main([]);
        //})
        //{
        //    IsBackground = true,
        //    Name = "Avalonia main thread"
        //};
        //if (OperatingSystem.IsWindows())
        //{
        //    thread.SetApartmentState(ApartmentState.STA);
        //}
        //thread.Start();
    }
}
