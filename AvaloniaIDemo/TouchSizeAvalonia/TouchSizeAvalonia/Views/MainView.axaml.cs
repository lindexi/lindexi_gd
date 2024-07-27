using Avalonia;
using Avalonia.Controls;

namespace TouchSizeAvalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.PointerMoved += MainView_PointerMoved;
    }

    private void MainView_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(null);
        var position = currentPoint.Position;
        dynamic d = currentPoint.Properties;
        Rect contactRect = d.ContactRect;
        MessageTextBlock.Text = $"X: {position.X}, Y: {position.Y} WH={contactRect.Width},{contactRect.Height}";
    }
}
