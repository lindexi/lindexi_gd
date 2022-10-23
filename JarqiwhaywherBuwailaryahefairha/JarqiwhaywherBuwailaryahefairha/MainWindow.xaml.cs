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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JarqiwhaywherBuwailaryahefairha;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var size = new Size(100, 100);

        var drawingGroup = new DrawingGroup();
        using (var drawingContext = drawingGroup.Open())
        {
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            for (int i = 0; i < 1000; i++)
            {
                var startPoint = new Point(Random.Shared.Next((int) (ActualWidth - size.Width)),
                    Random.Shared.Next((int) (ActualHeight - size.Height)));
                var endPoint = new Point(Random.Shared.Next((int) (ActualWidth - size.Width)),
                    Random.Shared.Next((int) (ActualHeight - size.Height)));

                var random = new byte[3];
                Random.Shared.NextBytes(random);
                var brush = new SolidColorBrush(Color.FromRgb(random[0], random[1], random[2]))
                {
                    Opacity = Random.Shared.NextDouble()
                };

                IEasingFunction? easingFunction = Random.Shared.Next(10) switch
                {
                    1 => new CubicEase(),
                    2 => new BounceEase(),
                    3 => new CircleEase(),
                    //4 => new ElasticEase(),
                    5 => new ExponentialEase(),
                    6 => new PowerEase(),
                    7 => new QuadraticEase(),
                    8 => new QuarticEase(),
                    9 => new SineEase(),
                    _ => null,
                };

                var rectAnimation = new RectAnimation(new Rect(startPoint, size), new Rect(endPoint, size),
                    new Duration(TimeSpan.FromSeconds(Random.Shared.Next(1, 100))))
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true,
                    EasingFunction = easingFunction,
                };

                var animationClock = rectAnimation.CreateClock();

                drawingContext.DrawRectangle(brush, null, new Rect(startPoint, size), animationClock);
            }
        }

        var drawingBrush = new DrawingBrush();
        drawingBrush.Drawing = drawingGroup;

        Background = drawingBrush;
    }
}
