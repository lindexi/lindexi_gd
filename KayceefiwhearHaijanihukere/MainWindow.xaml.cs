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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KayceefiwhearHaijanihukere
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
            var storyboard = new Storyboard();

            var doubleAnimation = new DoubleAnimation();
            Storyboard.SetTargetName(doubleAnimation, nameof(ButtonTranslateTransform));
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(TranslateTransform.XProperty));

            doubleAnimation.To = 100;
            doubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));

            var storyboardName = "s" + storyboard.GetHashCode();
            // 加入到字典，让 Storyboard 和 ButtonTranslateTransform 在相同的一个 NameScope 里
            Resources.Add(storyboardName, storyboard);

            storyboard.Children.Add(doubleAnimation);
            storyboard.Begin();
        }
    }
}
