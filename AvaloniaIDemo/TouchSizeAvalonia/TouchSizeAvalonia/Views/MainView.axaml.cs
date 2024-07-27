using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

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

        if (contactRect.Width > 0 && contactRect.Height > 0)
        {
            var translateTransform = (TranslateTransform) FooBorder.RenderTransform!;
            translateTransform.X = position.X - contactRect.Width / 2;
            translateTransform.Y = position.Y - contactRect.Height / 2;

            FooBorder.Width = contactRect.Width;
            FooBorder.Height = contactRect.Height;

            MessageTextBlock.Text = $"X: {position.X}, Y: {position.Y} WH={contactRect.Width},{contactRect.Height}";
        }
    }
}
