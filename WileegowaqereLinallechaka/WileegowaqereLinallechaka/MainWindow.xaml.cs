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

            for (int i = 0; i < 10000; i++)
            {
                StackPanel.Children.Add(new TextBlock()
                {
                    Text = i.ToString()
                });
            }

            var transform = new TranslateTransform();
            StackPanel.RenderTransform = transform;
            _transform = transform;

            DataContext = this;

            PointerBasedManipulationHandler manipulationHandler = new PointerBasedManipulationHandler()
            {
            };

            manipulationHandler.TranslationUpdated += ManipulationHandler_TranslationUpdated;

            PointerBasedManipulationHandler = manipulationHandler;

            PresentationSource.AddSourceChangedHandler(this, HandleSourceUpdated);

            SourceInitialized += MainWindow_SourceInitialized;

            SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PointerBasedManipulationHandler.InitializeDirectManipulation(e.NewSize);
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
        
        }

        private void ManipulationHandler_TranslationUpdated(float arg1, float arg2)
        {
            _transform.Y += arg2;
        }

        public List<string> Foo { set; get; } = new List<string>();
      
        private TranslateTransform _transform;

        private void HandleSourceUpdated(object sender, SourceChangedEventArgs e)
        {
            if (PointerBasedManipulationHandler != null && e.NewSource is System.Windows.Interop.HwndSource newHwnd)
                PointerBasedManipulationHandler.HwndSource = newHwnd;
        }

        private PointerBasedManipulationHandler PointerBasedManipulationHandler { get; set; }
    }
}
