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
    }

    private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        TheTextBlock.Text = $"abc{Random.Shared.Next(10)}";
        InvalidateVisual();
        UpdateLayout();
    }
}