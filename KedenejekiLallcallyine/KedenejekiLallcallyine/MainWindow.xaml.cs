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

namespace KedenejekiLallcallyine
{
    public class ASetting
    {
        public string Ax { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var t = new TextBox()
            {
                Name = null
            };
            var data = new ASetting();
            //When I bind control name alway null
            //t.SetBinding(NameProperty, new Binding("Ax") { Source = data });
            //I set an error binding, it must throw NullReferenceException
            t.SetBinding(TextBox.AcceptsReturnProperty, new Binding
            {
                Source = data,
                Path = new PropertyPath("aaa")
            });
        }
    }
}
