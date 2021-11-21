using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace KawhewurereliceeLearfaybeburwhe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HostVisual hostVisual = new HostVisual();
            _visualTarget = new VisualTarget(hostVisual);
            _hostVisual = hostVisual;

            var visualWrapper = new VisualWrapper(hostVisual);
            HostCanvas.Children.Add(visualWrapper);

            Task.Run(() =>
            {
                DrawingVisual drawingVisual = new DrawingVisual();
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 100, 100));
                }

                _hostVisual.Children.Add(drawingVisual);
            });
        }

        private HostVisual _hostVisual;

        private VisualTarget _visualTarget;
    }

    public class VisualTargetPresentationSource : PresentationSource
    {
        private VisualTarget _visualTarget;

        public override bool IsDisposed => throw new NotImplementedException();

        public override Visual RootVisual
        {
            set { }
            get => _visualTarget.RootVisual;
        }

        protected override CompositionTarget GetCompositionTargetCore()
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty("Child")]

    public class VisualWrapper : FrameworkElement
    {
        public VisualWrapper(Visual child)
        {
            _child = child;
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _child;
        private readonly Visual _child;
    }
}
