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

namespace ChairjuchiwhiRinehawwheago
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

        private MediaPlayer? MediaPlayer { set; get; }

        private void Grid_OnDrop(object sender, DragEventArgs e)
        {
            MediaPlayer?.Close();

            var fileList = (string[]?) e.Data.GetData(DataFormats.FileDrop);

            if (fileList is not null && fileList.Length > 0)
            {
                var mediaPlayer = MediaPlayer = new MediaPlayer();
                mediaPlayer.Open(new Uri(fileList[0]));

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
}
