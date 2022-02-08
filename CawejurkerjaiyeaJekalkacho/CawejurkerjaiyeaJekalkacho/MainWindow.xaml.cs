using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

namespace CawejurkerjaiyeaJekalkacho
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

        private void MemoryButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i < 1024 * 1024 / 10; i++)
                {
                    var value = new byte[1024];
                    value[1000] = 123;
                    _byteList.AddLast(value);
                }
            }
            catch (Exception exception)
            {

            }
        }

        private readonly LinkedList<byte[]> _byteList = new LinkedList<byte[]>();
    }
}
