namespace WifaberfearnairWherneyeaquqaki.MauiControls;

public partial class EmbeddedControl : ContentView
{
    public EmbeddedControl()
    {
        InitializeComponent();
    }

    private int count=0;
    public void CounterClicked(object sender, EventArgs e)
    {
        CounterButton.Text = ++count switch
        {
            1 => "Pressed Once!",
            _ => $"Pressed {count} times!"
        };
    }
}
