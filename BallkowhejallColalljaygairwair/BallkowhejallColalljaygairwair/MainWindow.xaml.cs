using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace BallkowhejallColalljaygairwair
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Canvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var toSize = 15;
            var currentSize = 10;

            var ellipse = new Ellipse()
            {
                Width = currentSize,
                Height = currentSize,
                Fill = Brushes.Gray
            };

            var point = e.GetPosition(Canvas);
            var translateTransform = new TranslateTransform(point.X, point.Y);
            ellipse.RenderTransform = translateTransform;
            Canvas.Children.Add(ellipse);

            var storyboard = new Storyboard();
            var widthAnimation = new DoubleAnimation(toValue: toSize, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("Width"));
            Storyboard.SetTarget(widthAnimation, ellipse);
            storyboard.Children.Add(widthAnimation);

            var heightAnimation = new DoubleAnimation(toValue: toSize, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
            Storyboard.SetTarget(heightAnimation, ellipse);
            storyboard.Children.Add(heightAnimation);

            var opacityAnimation = new DoubleAnimation(toValue: 0, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            Storyboard.SetTarget(opacityAnimation, ellipse);
            storyboard.Children.Add(opacityAnimation);

            // ( ToWidth(15) - CurrentWidth(10) ) / 2 = 2.5
            var translateTransformX = translateTransform.X - (toSize - currentSize) / 2;
            var xAnimation = new DoubleAnimation(toValue: translateTransformX, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(xAnimation,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            Storyboard.SetTarget(xAnimation, ellipse);
            storyboard.Children.Add(xAnimation);

            var translateTransformY = translateTransform.Y - (toSize - currentSize) / 2;
            var yAnimation = new DoubleAnimation(toValue: translateTransformY, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(yAnimation,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            Storyboard.SetTarget(yAnimation, ellipse);
            storyboard.Children.Add(yAnimation);

            storyboard.Completed += (o, args) => { Canvas.Children.Remove(ellipse); };
            storyboard.Begin();
        }
    }
}