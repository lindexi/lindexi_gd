using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;

namespace NaryonabaljijeaChayrefaynairjeawhaihel.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        PointerMoved += MainView_PointerMoved;
    }

    private void MainView_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        Thread.Sleep(100);
        var point = e.GetCurrentPoint(this);
        var translateTransform = (TranslateTransform) Rectangle.RenderTransform!;
        translateTransform.X = point.Position.X;
        translateTransform.Y = point.Position.Y;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }
}
