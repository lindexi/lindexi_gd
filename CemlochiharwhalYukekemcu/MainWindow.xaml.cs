using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace WpfApp1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		[DllImport("User32.dll")]
		private static extern nuint WindowFromPoint(int x, int y);
		[DllImport("User32.dll")]
		private static extern void GetWindowThreadProcessId(nuint hwnd, out int ID);
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Cross_CrossReleased(object sender, Controls.CrossReleasedEventArgs e)
		{
			Point p = (sender as Control).PointToScreen(e.Point);
			nuint hwnd = WindowFromPoint((int)p.X, (int)p.Y);
			GetWindowThreadProcessId(hwnd, out int pid);
			var process = Process.GetProcessById(pid);
			MessageBox.Show($"locaition = {p}, processId = {pid}, name = {process.ProcessName}");
		}
	}
}
