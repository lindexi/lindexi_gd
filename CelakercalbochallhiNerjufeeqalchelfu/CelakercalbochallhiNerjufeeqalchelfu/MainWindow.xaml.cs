using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CelakercalbochallhiNerjufeeqalchelfu
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            Binding binding = new Binding
            {
                Path = new PropertyPath("Property1"),
                Mode = BindingMode.Default
            };

            BindingOperations.SetBinding(this, TwoWayProperty, binding);

            binding = new Binding
            {
                Path = new PropertyPath("Property2"),
                Mode = BindingMode.Default
            };

            BindingOperations.SetBinding(this, OneWayProperty, binding);
        }

        public string Property1
        {
            get => _property; 
            set
            {
                _property = value;
                OnPropertyChanged();
            }
        }

        public string Property2
        {
            get => _property2; 
            set
            {
                _property2 = value;
                OnPropertyChanged();
            }
        }

        public string TwoWay
        {
            get { return (string) GetValue(TwoWayProperty); }
            set { SetValue(TwoWayProperty, value); }
        }


        public static readonly DependencyProperty TwoWayProperty =
            DependencyProperty.Register("TwoWay", typeof(string), typeof(MainWindow), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string OneWay
        {
            get { return (string) GetValue(OneWayProperty); }
            set { SetValue(OneWayProperty, value); }
        }

        public static readonly DependencyProperty OneWayProperty =
            DependencyProperty.Register("OneWay", typeof(string), typeof(MainWindow), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsArrange));
       
        private string _property;
        private string _property2;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Random ran = new Random();
            Text.Text = ran.Next().ToString();
            OneWay = Text.Text;
            TwoWay = Text.Text;
        }
    }
}
