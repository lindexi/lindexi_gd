using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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



		/// <summary>
		///		Creates a new image from file, rotates it
		///		and copies the result to the <see cref="origImage"/>
		///		control.
		/// </summary>
		/// <param name="filePath">
		///		Absolute file path to image file.
		/// </param>
		private void ProcessImageAsync(string filePath)
		{
			TransformedBitmap tb = new TransformedBitmap(new BitmapImage(new Uri(filePath)), new RotateTransform(90));

			CopyBitmapSourceToUi(tb);

			_ = new WriteableBitmap(tb);
		}

		/// <summary>
		///		Copies the provided <see cref="BitmapSource"/> object
		///		and displays it in the <see cref="origImage"/> control,
		///		using the UI thread.
		/// </summary>
		/// <param name="image">
		///		The <see cref="BitmapSource"/> object to display.
		/// </param>
		private void CopyBitmapSourceToUi(BitmapSource image)
		{
			BitmapSource uiSource;

			uiSource = BitmapFrame.Create(image);
			uiSource.Freeze();  // locks the bitmap source, so other threads can access

			Dispatcher.Invoke(() => origImage.Source = uiSource);
			//Thread.Sleep(10);   // WPF needs time to render the bitmap. During this period, creating a WriteableBitmap makes the program hang.
		}



		private void BrowseFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openDialog = new() { DefaultExt = ".png", FileName = Path.GetFileName(FilePath.Text), InitialDirectory = Path.GetDirectoryName(FilePath.Text), Filter = "Image files|*.bmp;*.jpg;*.jpeg;*.png;*.tiff" };

			if (openDialog.ShowDialog(this) == true)
			{
				FilePath.ToolTip = FilePath.Text = openDialog.FileName;

				origImage.Source = new BitmapImage(new Uri(openDialog.FileName));

				Task.Run(() => ProcessImageAsync(openDialog.FileName));
			}
		}
	}
}
