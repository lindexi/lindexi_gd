using Microsoft.UI.Xaml.Input;

namespace NeejabaykugoWerjekeeja;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void UIElement_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var intermediatePoints = e.GetIntermediatePoints(null);

        LogMessageTextBlock.Text = $"[{Environment.TickCount64}] Count of IntermediatePoints={intermediatePoints.Count}";

        Thread.Sleep(100);
    }
}
