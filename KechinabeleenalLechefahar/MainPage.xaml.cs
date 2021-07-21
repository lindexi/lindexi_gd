using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace KechinabeleenalLechefahar
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(Grid);

            var pngFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Path.GetRandomFileName() + ".jpg");
            using (var pngStream = await pngFile.OpenStreamForWriteAsync())
            {
                BitmapPropertySet propertySet = new BitmapPropertySet();
                BitmapTypedValue qualityValue = new BitmapTypedValue(0.77, PropertyType.Single);
                propertySet.Add("ImageQuality", qualityValue);

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, pngStream.AsRandomAccessStream(), propertySet);

                // https://docs.microsoft.com/en-us/windows/win32/properties/windows-properties-system?WT.mc_id=WD-MVP-5003260
                propertySet = new BitmapPropertySet();
                // 作者
                propertySet.Add("System.Author", new BitmapTypedValue("lindexi", PropertyType.String));
                // 相机型号
                propertySet.Add("System.Photo.CameraModel", new BitmapTypedValue("lindexi", PropertyType.String));
                // 制造商
                propertySet.Add("System.Photo.CameraManufacturer", new BitmapTypedValue("lindexi manufacturer", PropertyType.String));
                // 光圈值 System.Photo.FNumberNumerator/System.Photo.FNumberDenominator
                propertySet.Add("System.Photo.FNumberNumerator", new BitmapTypedValue(1, PropertyType.UInt32));
                propertySet.Add("System.Photo.FNumberDenominator", new BitmapTypedValue(10, PropertyType.UInt32));

                await encoder.BitmapProperties.SetPropertiesAsync(propertySet);

                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(pixelBuffer, BitmapPixelFormat.Bgra8, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight);
                encoder.SetSoftwareBitmap(softwareBitmap);


                await encoder.FlushAsync();

                softwareBitmap.Dispose();
            }

            await Launcher.LaunchFolderAsync(ApplicationData.Current.TemporaryFolder);
        }
    }
}
