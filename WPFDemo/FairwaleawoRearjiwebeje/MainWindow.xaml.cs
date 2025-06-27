using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace FairwaleawoRearjiwebeje;

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
        var windowInteropHelper = new WindowInteropHelper(this);
        var screen = Screen.FromHandle(windowInteropHelper.Handle);
        var pixelWorkingArea = screen.WorkingArea;
        var monitorSize = new Size(pixelWorkingArea.Width, pixelWorkingArea.Height);

        var root = VisualRoot(ContentGrid);
        Matrix transform = ((MatrixTransform) ContentGrid.TransformToAncestor(root)).Value;

        // 外部缩放。
        var compositionTarget = PresentationSource.FromVisual(ContentGrid)?.CompositionTarget;
        if (compositionTarget != null)
        {
            transform.Append(compositionTarget.TransformToDevice);
        }

        var rect = new Rect(new Size(1, 1));
        rect.Transform(transform);

        var slideMonitorRadio = rect.Size;

        var widthRadio = 1d / 2.6d;
        var heightRadio = 1d / 5d;

        var width = widthRadio * monitorSize.Width / slideMonitorRadio.Width;
        var height = heightRadio * monitorSize.Height / slideMonitorRadio.Height;

        var eraserWidth = width;
        var eraserHeight = height;

        //确保宽高比为2:3
        if (eraserHeight < 1.5 * eraserWidth)
        {
            eraserHeight = 1.5 * eraserWidth;
        }
        else
        {
            eraserWidth = eraserHeight / 1.5;
        }

        // 最大像素大小为屏幕的 1/3 尺寸
        const double maxScale = 1d / 3d;
        var maxWidth = monitorSize.Width / slideMonitorRadio.Width * maxScale;
        var maxHeight = monitorSize.Height / slideMonitorRadio.Height * maxScale;

        Size? maxEraserSize = new Size(maxWidth, maxHeight);

        // 当尺寸大于的时候，确保宽度高度比值
        if (eraserWidth > maxEraserSize.Value.Width)
        {
            eraserHeight = eraserHeight * maxEraserSize.Value.Width / eraserWidth;
            eraserWidth = maxEraserSize.Value.Width;
        }
        
        if (eraserHeight > maxEraserSize.Value.Height)
        {
            eraserWidth = eraserWidth * maxEraserSize.Value.Height / eraserHeight;
            eraserHeight = maxEraserSize.Value.Height;
        }

        ContentGrid.Children.Add(new Rectangle()
        {
            Width = eraserWidth,
            Height = eraserHeight,

            Fill = Brushes.Black,

            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        });
    }

    internal static Visual VisualRoot(Visual visual)
    {
        if (visual == null) throw new ArgumentNullException(nameof(visual));

        var root = visual;
        var parent = VisualTreeHelper.GetParent(visual);
        while (parent != null)
        {
            if (parent is Visual r)
            {
                root = r;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }

        return root;
    }
}