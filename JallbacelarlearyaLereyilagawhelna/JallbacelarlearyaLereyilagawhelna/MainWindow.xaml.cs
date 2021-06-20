using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JallbacelarlearyaLereyilagawhelna
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            MouseDown += MainWindow_MouseDown;
            MouseUp += MainWindow_MouseUp;
            Activated += MainWindow_Activated;
            Deactivated += MainWindow_Deactivated;
            LostFocus += MainWindow_LostFocus;

            Window w = null;
            var m = (MainWindow) w;

            DispatcherTimer d = new DispatcherTimer();
            d.Interval = TimeSpan.FromMilliseconds(100);
            d.Tick += D_Tick;
            d.Start();
            bool isShowed;

            WeakReference<Window> _storage = new WeakReference<Window>(this);
        }

        private void D_Tick(object sender, EventArgs e)
        {
        }

        private bool WithinCurrentToolTip2(DependencyObject o)
        {
            // If no current tooltip, then no need to look
            if (_currentToolTip == null)
            {
                return false;
            }

            DependencyObject v = o as Visual;
            if (v == null)
            {
                ContentElement ce = o as ContentElement;
                if (ce != null)
                {
                    v = FindContentElementParent(ce);
                }
                else
                {
                    v = o as Visual3D;
                }
            }

            return (v != null) &&
                   ((v is Visual && ((Visual)v).IsDescendantOf(_currentToolTip)) ||
                    (v is Visual3D && ((Visual3D)v).IsDescendantOf(_currentToolTip)));
        }

        private bool WithinCurrentToolTip(DependencyObject o)
        {
            // If no current tooltip, then no need to look
            if (_currentToolTip == null)
            {
                return false;
            }

            if (o is Visual v)
            {
                return v.IsDescendantOf(_currentToolTip);
            }

            if (o is ContentElement ce)
            {
                var contentElementParent = FindContentElementParent(ce);
                if (contentElementParent is Visual visual)
                {
                    return visual.IsDescendantOf(_currentToolTip);
                }
                else if (contentElementParent is Visual3D visual3D)
                {
                    return visual3D.IsDescendantOf(_currentToolTip);
                }
            }
            else if (o is Visual3D visual3D)
            {
                return visual3D.IsDescendantOf(_currentToolTip);
            }

            return false;
        }

        private DependencyObject FindContentElementParent(ContentElement ce)
        {
            throw new NotImplementedException();
        }


        private DependencyObject _currentToolTip;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var window1 = new Window1();
            window1.Show();
        }

        private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"MainWindow_MouseUp");
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"MainWindow_MouseDown");
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            Debug.WriteLine($"MainWindow_Activated");
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Debug.WriteLine($"MainWindow_Deactivated");
        }

        private void MainWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"MainWindow_LostFocus");
        }

        private void Window1_Foo(object sender, EventArgs e)
        {

        }
    }
}