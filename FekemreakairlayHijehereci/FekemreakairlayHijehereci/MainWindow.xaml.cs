using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace FekemreakairlayHijehereci;
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
        var outCanvas = this;
        Dispatcher.Invoke(new Action(() =>
        {
            Dwidth = (int) outCanvas.ActualWidth;
            Dheight = (int) outCanvas.ActualHeight;
            DisplayImage.Width = Dwidth;
            DisplayImage.Height = Dheight;
            wBitmap = new WriteableBitmap((int) Dwidth, (int) Dheight, 72, 72, PixelFormats.Bgr24, null);
            DisplayImage.Source = wBitmap;
            backBitmap = new Bitmap((int) Dwidth, (int) Dheight, wBitmap.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, wBitmap.BackBuffer);

            graphics = Graphics.FromImage(backBitmap);
            graphics.Clear(System.Drawing.Color.White);//整张画布置为白色
        }));
        Init();

        void Init()
        {
            Task.Run(() =>
            {
                if (Dwidth > 0 && Dheight > 0)
                {
                    while (true)
                    {
                        Thread.Sleep(30);
                        //画一些随机线
                        Random rand = new Random();
                        var stopwatch = Stopwatch.StartNew();
                        for (int i = 0; i < 100000; i++)
                        {
                            int x1 = rand.Next((int) Dwidth);
                            //int x2 = rand.Next(width);
                            int y1 = rand.Next(Dheight);
                            //int y2 = rand.Next(height);
                            //graphics.DrawLine(Pens.Red, x1, y1, x2, y2);
                            if (i % 2 == 0)
                            {
                                graphics.FillRectangle(System.Drawing.Brushes.Green, new System.Drawing.Rectangle(x1, y1, 20, 20));
                            }
                            else
                            {
                                graphics.FillRectangle(System.Drawing.Brushes.DarkGreen, new System.Drawing.Rectangle(x1, y1, 20, 20));
                            }
                        }

                        graphics.Flush();
                        //graphics.Dispose();
                        //graphics = null;
                        backBitmap.Dispose();

                        stopwatch.Stop();
                        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
                        // backBitmap = null;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            wBitmap.Lock();
                            wBitmap.AddDirtyRect(new Int32Rect(0, 0, Dwidth, Dheight));
                            wBitmap.Unlock();
                        }));
                    }
                }
            });
        }
    }

    WriteableBitmap wBitmap;
    Bitmap backBitmap = null;
    Graphics graphics = null;
    int Dwidth = 0;
    int Dheight = 0;
}
