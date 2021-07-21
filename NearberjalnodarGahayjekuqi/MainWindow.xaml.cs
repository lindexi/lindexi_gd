using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace NearberjalnodarGahayjekuqi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [DebuggerDisplay("{" + nameof(Debug) + "}")]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Foo();
        }

        private async void Foo()
        {
            while (true)
            {
                await Task.Delay(1000);

            }
        }

        public string Debug
        {
            get
            {
                StackPanel.Children.Add(new TextBlock()
                {
                    Text = "123"
                });
                return "Foo";
            }
        }
    }
}
