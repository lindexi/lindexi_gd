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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KayikerjalNeaheegeya
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

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var doubleAnimation = new DoubleAnimation
            {
                To = 100,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };
            Storyboard.SetTargetName(doubleAnimation, nameof(ButtonTranslateTransform));
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(TranslateTransform.XProperty));

            var storyboard = new Storyboard();
            storyboard.Children.Add(doubleAnimation);

            var storyboardName = nameof(Storyboard) + storyboard.GetHashCode();
            Resources.Add(storyboardName, storyboard);
            storyboard.Begin();
        }
    }
}
