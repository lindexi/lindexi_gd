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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BojakallhoLajacenu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Time.TimeFormat = "HH:mm:ss.fff";
            Time.SelectedTime = new DateTime(2020,1,2,0,0,0);
        }

        private void Border_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            AddPopup(sender);
        }

        private void Border_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            AddPopup(sender);
        }

        private void AddPopup(object sender)
        {
            var popup = new Popup
            {
                Child = new Border()
                {
                    Width = 100,
                    Height = 100,
                    Background = Brushes.Gray
                },

                IsOpen = true,
                StaysOpen = false,
                PlacementTarget = (UIElement)sender,
                Placement = PlacementMode.Center
            };
        }
    }
}