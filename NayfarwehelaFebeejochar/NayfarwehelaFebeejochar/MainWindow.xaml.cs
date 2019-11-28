using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NayfarwehelaFebeejochar
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void UIElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("PreviewMouseDown");
        }

        private void OpenPopup_OnClick(object sender, RoutedEventArgs e)
        {
            Popup.PlacementTarget = (UIElement) sender;
            Popup.Placement = PlacementMode.Mouse;
            Popup.IsOpen = true;
        }
    }
}
