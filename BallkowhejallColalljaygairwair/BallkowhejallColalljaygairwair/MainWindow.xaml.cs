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

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UIElement control = TextBlock;

            var top = control.TranslatePoint(new Point(), StackPanel);
            // 控件的宽度和高度
            var controlBounds = new Rect(top, control.DesiredSize);

            // 用户可以看到的大小
            var viewBounds = new Rect(new Point(e.HorizontalOffset, e.VerticalOffset),
                new Size(e.ViewportWidth, e.ViewportHeight));

            if (viewBounds.IntersectsWith(controlBounds))
            {
                Debug.WriteLine("歪楼");
            }
            else
            {
                Debug.WriteLine("不歪楼");
            }
        }
    }
}