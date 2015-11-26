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

            _viewaddressBook = new viewaddressBook(view,view._model);
            xaddressBook.DataContext = _viewaddressBook;

            _viewdiary = new viewdiary(view,view._model);
            xdiary.DataContext = _viewdiary;

            _viewmemorandum = new viewmemorandum(view,view._model);
            xmemorandum.DataContext = _viewmemorandum;

            _viewproperty = new viewproperty(view,view._model);
            xproperty.DataContext = _viewproperty;

            _view.form(visibilityform.reminder);
            Title = title;
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
        private viewmemorandum _viewmemorandum;
        private viewproperty _viewproperty;
        private string title
        {
            set;
            get;
        } = " 个人信息";
        private void addressbookvisibility(object sender , RoutedEventArgs e)
        {
            _view.form(visibilityform.addressbook);
            Title = "通讯录" + title;
        }

        private void memorandumvisibility(object sender , RoutedEventArgs e)
        {
            _view.form(visibilityform.memorandum);
            Title = "备忘录" + title;
        }

        private void diaryvisibility(object sender , RoutedEventArgs e)
        {
            _view.form(visibilityform.diary);
            Title = "日记" + title;
        }

        private void propertyvisibility(object sender , RoutedEventArgs e)
        {
            _view.form(visibilityform.property);
            Title = "个人财物" + title;
        }

        private void remindervisibility(object sender , RoutedEventArgs e)
        {
            _view.form(visibilityform.reminder);
            Title = title;
        }
    }
}
