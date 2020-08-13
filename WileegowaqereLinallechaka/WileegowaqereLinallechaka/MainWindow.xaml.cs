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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WileegowaqereLinallechaka
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //for (int i = 0; i < 10000; i++)
            //{
            //    StackPanel.Children.Add(new TextBlock()
            //    {
            //        Text = i.ToString()
            //    });
            //}

           var transform = new ScaleTransform(1, 1);
            MainGrid.RenderTransform = transform;
            _transform = transform;

            DataContext = this;

            PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);

            _manipulationHandler.ScaleUpdated += ManipulationHandler_ScaleUpdated;

            SizeChanged += MainWindow_SizeChanged;
        }

        private readonly PointerBasedManipulationHandler _manipulationHandler = new PointerBasedManipulationHandler();

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _manipulationHandler.InitializeDirectManipulation(e.NewSize);
        }

        private void ManipulationHandler_ScaleUpdated(float newScale)
        {
            _transform.ScaleX = _transform.ScaleY = newScale;
        }

        void OnSourceChanged(object sender, SourceChangedEventArgs e)
        {
            Console.WriteLine("Foo");
            if (e.NewSource is HwndSource source)
                _manipulationHandler.HwndSource = source;
        }

        public List<string> Foo { set; get; } = new List<string>();
      
        private ScaleTransform _transform;

    }
}
