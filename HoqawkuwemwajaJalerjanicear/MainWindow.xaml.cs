using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HoqawkuwemwajaJalerjanicear
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class CustomControl : Grid
    {
        public CustomControl()
        {
            Background = Brushes.White;
        }

        public StylusPlugInCollection StylusPlugInCollection => StylusPlugIns;
    }

    public class StylusPlugIn1 : StylusPlugIn
    {
        public StylusPlugIn1()
        {
        }

        public string Name { set; get; }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            Debug.WriteLine($"{Name} Down 当前线程{Thread.CurrentThread.Name} id={Thread.CurrentThread.ManagedThreadId}");
            base.OnStylusDown(rawStylusInput);
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}