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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KufayunurharnaLuragaruker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri("pack://application:,,,/KufayunurharnaLuragaruker;component/Video.mp4"));

            var videoDrawing = new VideoDrawing()
            {
                Player = mediaPlayer,
                Rect = new Rect(new Size(Width, Height))
            };
            var drawingBrush = new DrawingBrush(videoDrawing);
            Background = drawingBrush;
            mediaPlayer.Play();
        }
    }
}
