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

namespace gameT
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {        
        public MainWindow()
        {
            InitializeComponent();
            xsource.DataContext = c.g_获得类();
            this.DataContext = c.g_获得类();

            rect = new Rectangle();

            rect.Fill = new SolidColorBrush(Colors.Red);

            rect.Width = 50;

            rect.Height = 50;

            rect.RadiusX = 5;

            rect.RadiusY = 5;

            Carrier.Children.Add(rect);

            Canvas.SetLeft(rect , 0);

            Canvas.SetTop(rect , 0);
            CompositionTarget.Rendering += new EventHandler(Timer_Tick);
        }

        private void Carrier_MouseLeftButtonDown(object sender , MouseButtonEventArgs e)
        {
            moveTo = e.GetPosition(Carrier);

            //创建移动动画

            Point p = e.GetPosition(Carrier);

            Storyboard storyboard = new Storyboard();

            //创建X轴方向动画

            DoubleAnimation doubleAnimation = new DoubleAnimation(

              Canvas.GetLeft(rect) ,

              p.X ,

              new Duration(TimeSpan.FromMilliseconds(500))

            );

            Storyboard.SetTarget(doubleAnimation , rect);

            Storyboard.SetTargetProperty(doubleAnimation , new PropertyPath("(Canvas.Left)"));

            storyboard.Children.Add(doubleAnimation);

            //创建Y轴方向动画

            doubleAnimation = new DoubleAnimation(

              Canvas.GetTop(rect) ,

              p.Y ,

              new Duration(TimeSpan.FromMilliseconds(500))

            );

            Storyboard.SetTarget(doubleAnimation , rect);

            Storyboard.SetTargetProperty(doubleAnimation , new PropertyPath("(Canvas.Top)"));

            storyboard.Children.Add(doubleAnimation);

            //将动画动态加载进资源内

            if (!Resources.Contains("rectAnimation"))
            {

                Resources.Add("rectAnimation" , storyboard);

            }

            //动画播放

            storyboard.Begin();
        }
        Rectangle rect;//创建一个方块作为演示对象     
        double speed = 1; //设置移动速度
        Point moveTo; //设置移动目标
        private void Timer_Tick(object sender , EventArgs e)
        {

            double rect_X = Canvas.GetLeft(rect);

            double rect_Y = Canvas.GetTop(rect);

            Canvas.SetLeft(rect , rect_X + (rect_X < moveTo.X ? speed : -speed));

            Canvas.SetTop(rect , rect_Y + (rect_Y < moveTo.Y ? speed : -speed));

        }

        private void time_button_click(object sender , RoutedEventArgs e)
        {
            c.g_获得类().time();            
            //xtsource.Text = c.g_获得类().write;
            //xtsource.Text = c.g_获得类().write;
        }
    }
}
