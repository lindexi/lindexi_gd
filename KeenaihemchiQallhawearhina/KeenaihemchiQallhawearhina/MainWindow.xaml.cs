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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KeenaihemchiQallhawearhina
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty VolumeNumberProperty = DependencyProperty.Register(
            "VolumeNumber", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double),PropertyChangedCallback,CoerceValueCallback));

        private static object CoerceValueCallback(DependencyObject d, object baseValue)
        {
            var value = (double)baseValue;
            if (value < 0)
            {
                value = 0;
            }

            if (value > 1)
            {
                value = 1;
            }

            return value;
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }

        public double VolumeNumber
        {
            get { return (double) GetValue(VolumeNumberProperty); }
            set { SetValue(VolumeNumberProperty, value); }
        }

        private void TouchPanel_OnStylusDown(object sender, StylusDownEventArgs e)
        {
            if (CurrentStylusId == null)
            {
                CurrentStylusId = e.StylusDevice.Id;

                OriginPosition = e.GetPosition(this);
                LastPosition = OriginPosition;
            }

            Debug.WriteLine(e.TapCount);
            Debug.WriteLine("TouchPanel_OnStylusDown");
        }

        private void TouchPanel_OnStylusMove(object sender, StylusEventArgs e)
        {
            if (e.StylusDevice.Id == CurrentStylusId)
            {
                var currentPosition = e.GetPosition(this);
                if (VolumeSliderPanel.Visibility != Visibility.Visible)
                {
                    if (Math.Abs(currentPosition.Y - OriginPosition.Y) > 5)
                    {
                        VolumeSliderPanel.Visibility = Visibility.Visible;
                    }
                }

                var y = currentPosition.Y - LastPosition.Y;
                // 使用 -y 是因为往上滑是加音量
                var addNumber = -y;
                const double speed = 200;
                addNumber /= speed;
                VolumeNumber += addNumber;

                LastPosition = currentPosition;
            }
        }

        private void TouchPanel_OnStylusUp(object sender, StylusEventArgs e)
        {
            Debug.WriteLine("TouchPanel_OnStylusUp");
            if (e.StylusDevice.Id == CurrentStylusId)
            {
                CurrentStylusId = null;
                VolumeSliderPanel.Visibility = Visibility.Hidden;
            }
        }

        private void TouchPanel_OnLostStylusCapture(object sender, StylusEventArgs e)
        {
            if (e.StylusDevice.Id == CurrentStylusId)
            {
                CurrentStylusId = null;
            }
        }

        private void TouchPanel_OnStylusLeave(object sender, StylusEventArgs e)
        {
            Debug.WriteLine("TouchPanel_OnStylusLeave");

            if (e.StylusDevice.Id == CurrentStylusId)
            {
                CurrentStylusId = null;
            }
        }

        private int? CurrentStylusId { set; get; }
        private Point OriginPosition { set; get; }
        private Point LastPosition { set; get; }
    }
}
