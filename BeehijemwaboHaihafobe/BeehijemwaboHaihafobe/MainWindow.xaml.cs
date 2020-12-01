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

namespace BeehijemwaboHaihafobe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TouchDown += MainWindow_TouchDown;
            TouchUp += MainWindow_TouchUp;
        }

        private void MainWindow_TouchUp(object sender, TouchEventArgs e)
        {
            _currentTouchCount--;
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            CaptureTouch(e.TouchDevice);

            if (_currentTouchCount == 0)
            {
                TouchDragMoveWindowHelper.DragMove(this);
            }

            _currentTouchCount++;
        }

        private uint _currentTouchCount;
    }
}