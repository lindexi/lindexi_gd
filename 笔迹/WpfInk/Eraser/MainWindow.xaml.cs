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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eraser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            _circleEraserManager = new CircleEraserManager(EraserEllipse);

            _circleEraserManager.SetLastPoint(new Point(200, 200));
            _circleEraserManager.Move(new Point(100, 100));

            _circleEraserManager.SetLastPoint(new Point(300 + 半径, 200 + 半径));

            TouchMove += EraserEllipse_TouchMove;
            MouseMove += MainWindow_MouseMove;
            MouseWheel += MainWindow_MouseWheel;
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            半径 += e.Delta / 10.0;

            半径 = Math.Max(1, 半径);

            _circleEraserManager.Move(半径);
            DrawElement.Data = _circleEraserManager.GetCurrentGeometry();
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(this);
            Move(point);
        }

        public double 半径 { get; set; } = 75;

        private readonly CircleEraserManager _circleEraserManager;

        private void EraserEllipse_TouchMove(object sender, TouchEventArgs e)
        {
            var point = e.GetTouchPoint(this);

            Move(point.Position);
        }

        private void Move(Point point)
        {
            _circleEraserManager.Move(point, 半径);
            DrawElement.Data = _circleEraserManager.GetCurrentGeometry();
        }
    }
}
