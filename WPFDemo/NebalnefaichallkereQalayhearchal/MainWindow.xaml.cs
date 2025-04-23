using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NebalnefaichallkereQalayhearchal;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        _bufferEnumerator = GetBuffer().GetEnumerator();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Image.Width = ActualWidth;
        Image.Height = ActualHeight;

        var width = (int)ActualWidth;
        var height = (int)ActualHeight;

        var writeableBitmap =
            new WriteableBitmap(width, (int)ActualHeight, 96, 96, PixelFormats.Bgra32, null);
        _writeableBitmap = writeableBitmap;
        Image.Source = writeableBitmap;

        for (int i = 0; i < 60; i++)
        {
            var buffer = new byte[4 * width * height];
            PixelBufferList.Add(buffer);
            FillBuffer(buffer, i);
        }

        Redraw();
    }

    private void FillBuffer(byte[] buffer, int index)
    {
        for (var i = 0; i < buffer.Length; i += 4)
        {
            buffer[i] = (byte)(10 * index);
            buffer[i + 1] = 0;
            buffer[i + 2] = 0xF1;
            buffer[i + 3] = 0xFF;

            Random.Shared.NextBytes(buffer.AsSpan(i, 3));

            buffer[i + 3] = 0xFF;
        }

        Debug.Assert(_writeableBitmap != null);
        var width = _writeableBitmap.PixelWidth;
        var height = _writeableBitmap.PixelHeight;

        for (int heightIndex = 100; heightIndex < Math.Min(height, 300); heightIndex++)
        {
            for (int widthIndex = 50; widthIndex < Math.Min(width, (index + 1) * (width / 60)); widthIndex++)
            {
                var span = buffer.AsSpan(heightIndex * width * 4 + widthIndex * 4, 3);
                span[0] = 0xFF;
                span[1] = 0x00;
                span[2] = 0x00;
            }
        }
    }

    private List<byte[]> PixelBufferList { get; } = [];
    private WriteableBitmap? _writeableBitmap;

    private readonly IEnumerator<byte[]> _bufferEnumerator;

    private IEnumerable<byte[]> GetBuffer()
    {
        while (true)
        {
            foreach (var buffer in PixelBufferList)
            {
                yield return buffer;
            }
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        Dispatcher.InvokeAsync(Redraw, DispatcherPriority.Input);
    }

    private void Redraw()
    {
        Debug.Assert(_writeableBitmap != null);

        _bufferEnumerator.MoveNext();
        var buffer = _bufferEnumerator.Current;
        _writeableBitmap.Lock();
        _writeableBitmap.WritePixels(new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight),
            buffer, 4 * _writeableBitmap.PixelWidth, 0);
        _writeableBitmap.Unlock();

        InvalidateVisual();
    }
}