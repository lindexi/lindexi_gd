namespace MauiApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        Loaded += MainPage_Loaded;
    }

    private int _count = 0;

    private void MainPage_Loaded(object sender, EventArgs e)
    {
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        _count++;

        if (_count == 1)
            CounterButton.Text = $"Clicked {_count} time";
        else
            CounterButton.Text = $"Clicked {_count} times";
    }
}

