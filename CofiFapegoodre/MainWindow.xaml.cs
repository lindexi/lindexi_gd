using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Cvte.RemoteObject;

namespace CofiFapegoodre
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

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var remoteProcessManager = new RemoteProcessManager(typeof(WasxayTembou).Assembly);

            // 连接
            remoteProcessManager.Connect();

            var baltrartularLouronay = remoteProcessManager.GetObject<WasxayTembou>();

            // 执行里面方法
            baltrartularLouronay.HemooZurmisdate();
        }
    }

    [Remote]
    public class WasxayTembou : MarshalByRefObject
    {
        public void HemooZurmisdate()
        {
            Console.WriteLine("DertaHofelbere");
        }
    }
}