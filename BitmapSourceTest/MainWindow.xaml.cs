using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BitmapSourceTest
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


		private void BrowseFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openDialog = new() { DefaultExt = ".png", FileName = Path.GetFileName(FilePath.Text), InitialDirectory = Path.GetDirectoryName(FilePath.Text), Filter = "Image files|*.bmp;*.jpg;*.jpeg;*.png;*.tiff" };

			if (openDialog.ShowDialog(this) == true)
			{
				FilePath.ToolTip = FilePath.Text = openDialog.FileName;

				Dispatcher backgroundDispatcher = null!;
				AutoResetEvent resetEvent = new AutoResetEvent(false);
				Thread thread = new Thread(() =>
				{
					backgroundDispatcher = Dispatcher.CurrentDispatcher;
					resetEvent.Set();
					Dispatcher.Run();
				});
				thread.SetApartmentState(ApartmentState.STA);
				thread.IsBackground = true;
				thread.Start();

				resetEvent.WaitOne();

				// To Create the MediaContext which is thread static
				backgroundDispatcher.InvokeAsync(() => new WriteableBitmap(1, 1, 96, 96, PixelFormats.Bgr32, null));

				backgroundDispatcher.InvokeAsync(() =>
				{
					var image = new BitmapImage(new Uri(openDialog.FileName));

					image.Freeze(); // locks the bitmap source, so other threads can access

					Dispatcher.InvokeAsync(() => Image.Source = (BitmapSource) image);

					_ = new WriteableBitmap(image);
				});
			}
		}
	}
}
