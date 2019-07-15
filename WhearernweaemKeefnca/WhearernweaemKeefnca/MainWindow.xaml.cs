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

namespace WhearernweaemKeefnca
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TouchDown += MainWindow_TouchDown;
            TouchUp += MainWindow_TouchUp;
            TouchMove += MainWindow_TouchMove;
        }

        private void MainWindow_TouchMove(object sender, TouchEventArgs e)
        {
            WriteToLog($"id {e.TouchDevice.Id} 移动");

            if (!TouchDownList.Contains(e.TouchDevice.Id))
            {
                Warn += $"移动过程发现 {e.TouchDevice.Id} 丢失";
            }

            Log();
        }

        private List<int> TouchDownList { get; } = new List<int>();
        private string Warn { set; get; }

        private void MainWindow_TouchUp(object sender, TouchEventArgs e)
        {
            WriteToLog($"---抬起 id = {e.TouchDevice.Id}\r\n");

            if (TouchDownList.Contains(e.TouchDevice.Id))
            {
                TouchDownList.Remove(e.TouchDevice.Id);
            }
            else
            {
                Warn += $"触摸不成对，抬起的 {e.TouchDevice.Id} 没有找到按下记录\r\n";
            }

            Log();
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            if (TouchDownList.Contains(e.TouchDevice.Id))
            {
                Warn += $"触摸不成对，已经存在 {e.TouchDevice.Id} 按下\r\n";
            }

            TouchDownList.Add(e.TouchDevice.Id);
            WriteToLog($"+++按下 id = {e.TouchDevice.Id}");
            Log();
        }

        private void WriteToLog(string str)
        {
            _logList.Add(
                $"{DateTime.Now.Minute.ToString("00")} : {DateTime.Now.Second.ToString("00")}.{DateTime.Now.Millisecond.ToString("000")} {str}");
            while (_logList.Count > 10)
            {
                _logList.RemoveAt(0);
            }

            Log();
        }

        private readonly List<string> _logList = new List<string>();

        private void Log()
        {
            var str = new StringBuilder();
            str.Append($"当前触摸总数 {TouchDownList.Count} \r\n");
            str.Append($"触摸id列表 {string.Join(",", TouchDownList)}\r\n");
            str.Append(Warn);
            str.Append("\r\n");
            str.Append("=====\r\n");
            foreach (var temp in _logList)
            {
                str.Append(temp);
                str.Append("\r\n");
            }

            Tracer.Text = str.ToString();
        }
    }
}