using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace CairjawworalhulalGeacharkucoha
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var d3DImage = D3DImage;
            var methodInfo = d3DImage.GetType().GetMethod("Callback", BindingFlags.NonPublic | BindingFlags.Instance);
            methodInfo!.Invoke(d3DImage, new object[] { false, (uint) 0 });
        }
    }
}
