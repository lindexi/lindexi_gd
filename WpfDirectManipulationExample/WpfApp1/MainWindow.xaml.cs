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
using System.Windows.Interop;
using Xamarin.Designer.Windows;

namespace WpfApp1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		ScaleTransform transform;
		double currentScaleX = 1.0, currentScaleY = 1.0;

		PointerBasedManipulationHandler manipulationHandler = new PointerBasedManipulationHandler();

		public MainWindow()
		{
			InitializeComponent();

			transform = new ScaleTransform(1, 1);
			mainGrid.RenderTransform = transform;
			/*mainGrid.IsManipulationEnabled = true;
			mainGrid.ManipulationStarting += MainGrid_ManipulationStarting;
			mainGrid.ManipulationStarted += MainGrid_ManipulationStarted;
			mainGrid.ManipulationDelta += MainGrid_ManipulationDelta;*/
			PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);
			manipulationHandler.ScaleUpdated += (newScale) => transform.ScaleX = transform.ScaleY = newScale;
			this.SizeChanged += MainWindow_SizeChanged;
			mainGrid.MouseLeftButtonUp += MainGrid_MouseLeftButtonUp;
		}

		private void MainGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			//manipulationHandler.Activate();
		}

		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//manipulationHandler.SetSize(e.NewSize);
			manipulationHandler.InitializeDirectManipulation(e.NewSize);
		}

		void OnSourceChanged (object sender, SourceChangedEventArgs e)
		{
			Console.WriteLine("Foo");
			if (e.NewSource is HwndSource source)
				manipulationHandler.HwndSource = source;
		}

		private void MainGrid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(e.DeltaManipulation.Scale);
			transform.CenterX = mainGrid.ActualWidth / 2;
			transform.CenterY = mainGrid.ActualHeight / 2;
			transform.ScaleX = e.DeltaManipulation.Scale.X;
			transform.ScaleY = e.DeltaManipulation.Scale.Y;
		}

		private void MainGrid_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
		{
			currentScaleX = currentScaleY = 1;
		}

		private void MainGrid_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
		{
			e.ManipulationContainer = mainGrid;
			e.Mode = ManipulationModes.Scale;
			e.Handled = true;
		}
	}
}
