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

namespace FawlalnejajerelaWhallgemcurkear
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var newTempFolder = @"D:\lindexi\无法访问文件夹";
            Environment.SetEnvironmentVariable("TEMP", newTempFolder);
            Environment.SetEnvironmentVariable("TMP", newTempFolder);

            var uri = new Uri("pack://application:,,,/Text.cur");
            var resource = Application.GetResourceStream(uri);
            Cursor = new Cursor(resource.Stream);
        }
    }
}
