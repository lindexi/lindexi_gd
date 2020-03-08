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

namespace KawhawcarbelYukojaybana
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Three = 3.0;
            Four = 3.0;

            DataContext = this;
        }

        public static readonly DependencyProperty FourProperty = DependencyProperty.Register(
            "Four", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double)));

        public double Four
        {
            get { return (double) GetValue(FourProperty); }
            set { SetValue(FourProperty, value); }
        }

        public static readonly DependencyProperty ThreeProperty = DependencyProperty.Register(
            "Three", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double)));

        public double Three
        {
            get { return (double) GetValue(ThreeProperty); }
            set { SetValue(ThreeProperty, value); }
        }
    }
}
