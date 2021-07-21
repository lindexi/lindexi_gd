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

namespace Walterlv.Demo.XamlProperties
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void DoubleAnimationUsingKeyFrames_Completed(object? sender, EventArgs e)
        {
            
        }

        public static readonly DependencyProperty TestBoolProperty = DependencyProperty.Register(
            "TestBool", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

        public bool TestBool
        {
            get { return (bool) GetValue(TestBoolProperty); }
            set { SetValue(TestBoolProperty, value); }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            TestBool = !TestBool;
        }
    }
}
