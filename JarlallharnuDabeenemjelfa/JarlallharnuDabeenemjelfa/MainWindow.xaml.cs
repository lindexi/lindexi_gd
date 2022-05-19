using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace JarlallharnuDabeenemjelfa
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
            var cnvMain = this;
            var allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();

            var src1x1 = BitmapSource.Create(1, 1, 72, 72, PixelFormats.BlackWhite, BitmapPalettes.BlackAndWhite, new byte[] { 0xFF }, 1);
            var src = new TransformedBitmap(src1x1, new ScaleTransform(2000, 2000));// here I get Exception
            cnvMain.Background = new ImageBrush(src);// cnvMain = Canvas

            var allocatedBytesForCurrentThreadAfter = GC.GetAllocatedBytesForCurrentThread();
            Debug.WriteLine($"Delta: {allocatedBytesForCurrentThreadAfter - allocatedBytesForCurrentThread}");
        }
    }
}
