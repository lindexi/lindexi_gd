using Avalonia.Controls;
using Avalonia.Input;

namespace NanujafakeJalelhalcall.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Grid_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        if (properties.IsLeftButtonPressed)
        {
            FooBorder.Width += 100;
            FooBorder.Height += 100;
        }
        else
        {
            FooBorder.Width -= 100;
            FooBorder.Height -= 100;
        }
    }
}
