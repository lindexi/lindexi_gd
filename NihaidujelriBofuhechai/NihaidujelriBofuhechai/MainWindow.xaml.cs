using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NihaidujelriBofuhechai;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        //Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        while (true)
        {
            await Task.Delay(100);
            ColorBrush = Brushes.Black;
            await Task.Delay(100);
            ColorBrush = Brushes.White;
        }
    }

    public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register(
        nameof(ColorBrush), typeof(Brush), typeof(MainWindow), new PropertyMetadata(default(Brush)));

    public Brush ColorBrush
    {
        get { return (Brush)GetValue(ColorBrushProperty); }
        set { SetValue(ColorBrushProperty, value); }
    }

    private void WhiteButton_OnClick(object sender, RoutedEventArgs e)
    {
        ColorBrush = ((Button)sender).Foreground;
    }

    private void BlackButton_OnClick(object sender, RoutedEventArgs e)
    {
        ColorBrush = ((Button)sender).Foreground;
    }

    private void DefaultButton_OnClick(object sender, RoutedEventArgs e)
    {
        ColorBrush = ((Button)sender).Foreground;
    }
}
