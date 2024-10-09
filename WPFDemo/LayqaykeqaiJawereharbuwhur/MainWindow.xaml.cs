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
using System.Windows.Threading;

namespace LayqaykeqaiJawereharbuwhur;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _f = new F();
        StylusPlugIns.Add(_f);

        Foo();
    }

    private readonly F _f;

    private async void Foo()
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            await Task.Delay(1000);
            int count;
            lock (_f)
            {
                count = _f.Count;
                _f.Count = 0;
            }

            stopwatch.Stop();
            if (count != 0)
            {
                var fps = count / stopwatch.Elapsed.TotalSeconds;
                var message = $"触摸报点： {fps}";
                LogTextBlock.Text = message;
            }
        }
    }

    class F : StylusPlugIn
    {
        public int Count { set; get; }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            lock(this)
            {
                Count++;
            }

            base.OnStylusMove(rawStylusInput);
        }
    }
}