using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.Manipulations;

namespace JarwefekaLawfakuwi
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ManipulationProcessor2D = new ManipulationProcessor2D(Manipulations2D.All);

            ManipulationProcessor2D.Started += ManipulationProcessor2D_Started;
            ManipulationProcessor2D.Delta += ManipulationProcessor2D_Delta;
            ManipulationProcessor2D.Completed += ManipulationProcessor2D_Completed;

            StylusDown += MainWindow_StylusDown;
            StylusMove += MainWindow_StylusMove;
            StylusUp += MainWindow_StylusUp;
        }

        private readonly Dictionary<int, (double x, double y)> _points = new();
        private int _lastTimeStamp;

        private ManipulationProcessor2D ManipulationProcessor2D { get; }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            var timestamp = e.Timestamp;
            if (timestamp <= _lastTimeStamp)
                //非递增时间戳+1，确保不丢Down
                timestamp = _lastTimeStamp + 1;
            _lastTimeStamp = timestamp;

            var point = e.GetStylusPoints(this);
            var firstStylusPoint = point.FirstOrDefault();
            _points.Add(e.StylusDevice.Id, new (firstStylusPoint.X, firstStylusPoint.Y));

            ManipulationProcessor2D.ProcessManipulators(timestamp,
                _points.Select(temp => new Manipulator2D(temp.Key, (float) temp.Value.x, (float) temp.Value.y)));
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            if (!_points.ContainsKey(e.StylusDevice.Id))
                // 丢失按下的，忽略
                return;

            var timestamp = e.Timestamp;
            if (timestamp <= _lastTimeStamp)
                //非递增时间戳直接忽略
                return;
            _lastTimeStamp = timestamp;

            // 下面代码和 e.GetPosition() 其实没差别
            var firstStylusPoint = e.GetStylusPoints(this).FirstOrDefault();
            _points[e.StylusDevice.Id] = new (firstStylusPoint.X, firstStylusPoint.Y);

            ManipulationProcessor2D.ProcessManipulators(timestamp,
                _points.Select(temp => new Manipulator2D(temp.Key, (float) temp.Value.x, (float) temp.Value.y)));
        }

        private void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            if (!_points.ContainsKey(e.StylusDevice.Id)) return;
            var timestamp = e.Timestamp;
            if (timestamp <= _lastTimeStamp)
                //非递增时间戳+1，确保不丢Up
                timestamp = _lastTimeStamp + 1;

            // 下面代码和 e.GetPosition() 其实没差别
            var firstStylusPoint = e.GetStylusPoints(this).FirstOrDefault();
            _points[e.StylusDevice.Id] = new (firstStylusPoint.X, firstStylusPoint.Y);

            ManipulationProcessor2D.ProcessManipulators(timestamp,
                _points.Select(temp => new Manipulator2D(temp.Key, (float) temp.Value.x, (float) temp.Value.y)));
            _points.Remove(e.StylusDevice.Id);

            if (_points.Count == 0)
            {
                ManipulationProcessor2D.CompleteManipulation(timestamp);
                _lastTimeStamp = 0;
            }
        }

        private void ManipulationProcessor2D_Started(object? sender, Manipulation2DStartedEventArgs e)
        {
        }

        private void ManipulationProcessor2D_Delta(object? sender, Manipulation2DDeltaEventArgs e)
        {
            // 获取的 Rotation 是弧度
            RotateTransform.Angle += e.Delta.Rotation * 180 / Math.PI;

            TranslateTransform.X += e.Delta.TranslationX;
            TranslateTransform.Y += e.Delta.TranslationY;

            ScaleTransform.ScaleX += e.Delta.ExpansionX / 100;
            ScaleTransform.ScaleY += e.Delta.ExpansionY / 100;
        }

        private void ManipulationProcessor2D_Completed(object? sender, Manipulation2DCompletedEventArgs e)
        {
        }
    }
}