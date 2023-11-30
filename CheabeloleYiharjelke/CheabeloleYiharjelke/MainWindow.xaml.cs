using System.Diagnostics;
using System.Text;
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

namespace CheabeloleYiharjelke;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.StylusPlugIns.Add(new StylusPlugIn1());
    }


    public class StylusPlugIn1 : StylusPlugIn
    {
        public StylusPlugIn1()
        {
        }

        public string Name { set; get; }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            Debug.WriteLine($"{Name} Down 当前线程={Thread.CurrentThread.Name} {Thread.CurrentThread.ManagedThreadId}");
            base.OnStylusDown(rawStylusInput);
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}