using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace ranPoint
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel.viewModel view;
        public MainWindow()
        {
            view = new ViewModel.viewModel();
            this.DataContext = view;
            InitializeComponent();
            
        }

        private void 确定(object sender , RoutedEventArgs e)
        {
            if (view._cancel)
            {
                Thread t = new Thread(view.coordinate);
                t.Name = view.name.ToString();
                t.Start();
            }
            else
            {
                view.reminder += "线程在运行\r\n";
            }
        }
        
    }
}
