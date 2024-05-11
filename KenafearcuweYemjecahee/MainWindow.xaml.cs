using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KenafearcuweYemjecahee
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

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var toggleButton = (ToggleButton) sender;

            FullScreenHelper.MarkFullscreenWindowTaskbarList(new WindowInteropHelper(this).Handle, toggleButton.IsChecked is true);

            //if (toggleButton.IsChecked is true)
            //{
            //    FullScreenHelper.StartFullScreen(this);
            //}
            //else
            //{
            //    FullScreenHelper.EndFullScreen(this);
            //}
        }

    }
}