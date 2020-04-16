using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Walterlv.Demo.Windows;

namespace BiwagerehuBofurnuca
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Dispatcher threadDispatcher = null;

            var autoResetEvent = new AutoResetEvent(false);

            var thread = new Thread(() =>
            {
                threadDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                autoResetEvent.Set();

                System.Windows.Threading.Dispatcher.Run();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            autoResetEvent.WaitOne();

            IntPtr handle = IntPtr.Zero;

            threadDispatcher.Invoke(() =>
            {
                var window = new Window()
                {
                    Content = new Foo()
                };

                var windowInteropHelper = new WindowInteropHelper(window);
                handle = windowInteropHelper.EnsureHandle();
                window.Show();
            });

            var windowWrapper = new WindowWrapper(handle);

            Grid.Children.Add(windowWrapper);
        }
    }

    public class Foo : Control
    {
        /// <inheritdoc />
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var point = e.GetPosition(this);
            _point = point;
            InvalidateVisual();

            base.OnMouseDown(e);
        }

        private Point _point;

        /// <inheritdoc />
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(Brushes.Gray, null, _point, 10, 10);
        }
    }
}
