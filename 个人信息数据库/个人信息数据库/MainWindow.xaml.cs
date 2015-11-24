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

namespace 个人信息数据库
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            _view = new viewModel();
            InitializeComponent();
            this.DataContext = view;
            //xl.ItemsSource = View.addressBook;
        }

        private viewModel view
        {
            set
            {
                _view = value;
            }
            get
            {
                return _view;
            }
        }

        private void Button_Click(object sender , RoutedEventArgs e)
        {
            view.ce();

            //View.readsql();
            xl.ItemsSource = view.addressBook;
        }
        private viewModel _view;

    }
}
