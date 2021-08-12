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

namespace ChefearkaigallladiYokecoba
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var codeBase = new Uri("file:///f:/Code/lindexi/ChefearkaigallladiYokecoba/ChefearkaigallladiYokecoba/bin/Debug/net6.0-windows/win-x86/ChefearkaigallladiYokecoba.dll");
            var filePath = "Image.png";
            if (codeBase.IsFile)
            {
            var combine = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(codeBase.LocalPath),filePath);
            }
            else
            {
            var combine = System.IO.Path.Combine(codeBase.LocalPath,filePath);

            }
            Uri file = new Uri(codeBase, filePath);

            InitializeComponent();
        }
    }
}
