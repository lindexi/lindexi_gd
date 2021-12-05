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

namespace CowhahiloRallkemnuke;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        while (true)
        {
            Speed = Math.Max(Speed, 1);

            var rotateValue = 0.1;
            var delayTime = 1000.0;

            delayTime = delayTime / Speed - Speed;

            delayTime = Math.Max(delayTime, 5);
           
            if(Speed > 10)
            {
                rotateValue += Speed / 10;
            }

            GridRotateTransform.Angle += rotateValue;

            await Task.Delay(TimeSpan.FromMilliseconds(delayTime));
        }
    }

    private void SpeedUpButton_Click(object sender, RoutedEventArgs e)
    {
        Speed++;
    }

    private void SpeedDownButton_Click(object sender, RoutedEventArgs e)
    {
        Speed--;
    }

    public double Speed
    {
        get { return (double)GetValue(SpeedProperty); }
        set { SetValue(SpeedProperty, value); }
    }

    public static readonly DependencyProperty SpeedProperty =
        DependencyProperty.Register("Speed", typeof(double), typeof(MainWindow), new PropertyMetadata(0.0));
}
