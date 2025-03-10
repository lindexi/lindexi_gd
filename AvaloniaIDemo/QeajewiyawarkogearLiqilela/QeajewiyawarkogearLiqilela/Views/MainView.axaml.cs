using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace QeajewiyawarkogearLiqilela.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var button = new Button()
        {
            Content = "µã»÷"
        };
        button.Click += Button_OnClick;
        RootStackPanel.Children.Add(button);
    }
}
