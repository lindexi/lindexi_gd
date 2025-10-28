using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

#pragma warning disable WPF0001

namespace LewerekeekalurcemFoqihairhilaw;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //ThemeMode = ThemeMode.Light;
    }

    private void SwitchThemeModeButton_OnClick(object sender, RoutedEventArgs e)
    {
        //if (SwitchThemeModeButton.IsChecked is true)
        //{
        //    ThemeMode = ThemeMode.Dark;
        //}
        //else
        //{
        //    ThemeMode = ThemeMode.Light;
        //}
    }
}