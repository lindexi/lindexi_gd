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

namespace NewhearfarairchaylucoLerhejuche
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

        public void Foo(object _)
        {
        }

        public void AddFoo(Action<object> action, object state)
        {
            ActionList.Add((action, state));
        }

        private List<(Action<object> action, object state)> ActionList { get; } =
            new List<(Action<object> action, object state)>();

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                AddFoo(Foo, null);
            }
        }

        private void Button2_OnClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                AddFoo(s => ((MainWindow) s).Foo(null), this);
            }
        }
    }
}