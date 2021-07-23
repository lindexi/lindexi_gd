using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BallbujawfemNolahelle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EraserCanvas.MouseDown += EraserCanvas_MouseDown;
            EraserCanvas.MouseMove += EraserCanvas_MouseMove;
            EraserCanvas.MouseUp += EraserCanvas_MouseUp;
        }

        private IncrementalStrokeHitTester _incrementalStrokeHitTester;
        private bool _isDown;

        private void EraserCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDown = true;

            IncrementalStrokeHitTester incrementalStrokeHitTester =
                InkCanvas.Strokes.GetIncrementalStrokeHitTester(new RectangleStylusShape(EraserShape.ActualWidth,
                    EraserShape.ActualHeight));

            _incrementalStrokeHitTester = incrementalStrokeHitTester;
            _incrementalStrokeHitTester.StrokeHit += IncrementalStrokeHitTester_StrokeHit;
        }

        private void EraserCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EraserCanvas.Visibility = Visibility.Collapsed;
            _isDown = false;

            _incrementalStrokeHitTester.EndHitTesting();
            _incrementalStrokeHitTester = null;
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            EraserCanvas.Visibility = Visibility.Visible;

            TranslateTransform.X = -1000;
            TranslateTransform.Y = -1000;
        }

        private void IncrementalStrokeHitTester_StrokeHit(object sender, StrokeHitEventArgs e)
        {
            InkCanvas.Strokes.Remove(e.HitStroke);
            InkCanvas.Strokes.Add(e.GetPointEraseResults());
        }

        private void EraserCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDown)
            {
                var point = e.GetPosition(this);
                TranslateTransform.X = point.X - EraserShape.ActualWidth / 2;
                TranslateTransform.Y = point.Y - EraserShape.ActualHeight / 2;

                _incrementalStrokeHitTester.AddPoint(point);
            }
        }
    }
}