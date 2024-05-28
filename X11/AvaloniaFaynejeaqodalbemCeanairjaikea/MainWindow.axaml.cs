using Avalonia.Controls;

namespace AvaloniaFaynejeaqodalbemCeanairjaikea;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        PointerMoved += MainWindow_PointerMoved;
    }

    private void MainWindow_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var currentPoint = e.GetCurrentPoint(this);
        TextBlock.Text = $"{TextBlock.Text}\r\nX={currentPoint.Position.X} Y={currentPoint.Position.Y} Pressure={currentPoint.Properties.Pressure}";
    }
}