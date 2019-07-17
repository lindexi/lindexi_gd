using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace Babukeelleneeoai
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var encoding = System.Text.Encoding.GetEncodings();
            foreach (var temp in encoding)
            {
                Debug.WriteLine(temp.GetEncoding().EncodingName);
            }
            var file = new FileInfo("../../MainWindow.xaml.cs");
            var str = file.FullName;
            new ConvertFileEncodingPage(file).Show();
        }
    }
}
