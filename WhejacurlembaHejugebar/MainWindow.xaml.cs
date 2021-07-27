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

namespace WhejacurlembaHejugebar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ExcrementYellow = Brushes.Red;
            DataContext = this;
            InitializeComponent();
        }

        public static readonly DependencyProperty ExcrementYellowProperty = DependencyProperty.Register(
            "ExcrementYellow", typeof(SolidColorBrush), typeof(MainWindow),
            new PropertyMetadata(default(SolidColorBrush)));

        public SolidColorBrush ExcrementYellow
        {
            get { return (SolidColorBrush) GetValue(ExcrementYellowProperty); }
            set { SetValue(ExcrementYellowProperty, value); }
        }
    }
}