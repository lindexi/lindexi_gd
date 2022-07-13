using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WeakEventManagerTest;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MyControl[] _myControls = new MyControl[100];

    Thread _thread;
    Thread _thread2;

    private MyList _list = new MyList();

    public MainWindow()
    {
        InitializeComponent();

        _thread = new Thread(() =>
        {
            while (true)
            {
                MyEventSource.Instance.SendEvent();
                Thread.Sleep(10);
            }

            ;
        });
        _thread.Start();

        _thread2 = new Thread(() =>
        {
            while (true)
            {
                _list.Add(new MyListItem());

                if (_list.Count > 100)
                {
                    _list.RemoveAt(0);
                }

                Thread.Sleep(10);
            }

            ;
        });
        _thread2.Start();
    }

    private void SendEvent_OnClick(object sender, RoutedEventArgs e)
    {
        
        for (int i = 0; i < _myControls.Length; i++)
        {
            _myControls[i] = new MyControl(_list);
        }

        Debug.WriteLine("new controls ready");
    }
}