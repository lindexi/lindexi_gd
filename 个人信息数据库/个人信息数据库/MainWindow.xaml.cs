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
using 个人信息数据库.ViewModel;
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

            _viewaddressBook = new viewaddressBook(view._model);
            xaddressBook.DataContext = _viewaddressBook;

            _viewdiary = new viewdiary(view._model);
            xdiary.DataContext = _viewdiary;
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
        


        private void principal_Computer(object sender , RoutedEventArgs e)
        {
            view.principal_computer();
        }

        private void slaveComputer(object sender , RoutedEventArgs e)
        {
            view.slave_computer();
        }
        private viewModel _view;
        private viewaddressBook _viewaddressBook;
        private viewdiary _viewdiary;
    }
}
