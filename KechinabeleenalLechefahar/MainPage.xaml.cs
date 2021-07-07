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
                propertySet.Add("f-number", new BitmapTypedValue(6f, PropertyType.Single));

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, pngStream.AsRandomAccessStream(), propertySet);

                /*var propertySet = new Windows.Graphics.Imaging.BitmapPropertySet();
                //propertySet.Add("Focal length", new BitmapTypedValue(Encoding.UTF8.GetBytes("20.3 mm"), PropertyType.UInt8Array));
                //propertySet.Add("Exposure time", new BitmapTypedValue(Encoding.UTF8.GetBytes("1/659 s "), PropertyType.UInt8Array));
                //propertySet.Add("F-Number", new BitmapTypedValue(6f, PropertyType.Single));
                //propertySet.Add("City", new BitmapTypedValue("London", PropertyType.String));
                //propertySet.Add("Focal Length in 35mm Format", new BitmapTypedValue("12", PropertyType.String));
                propertySet.Add("ImageQuality", new BitmapTypedValue(0.1f, PropertyType.Single));
                propertySet.Add("System.Photo.Orientation", new Windows.Graphics.Imaging.BitmapTypedValue
                (
                    1, // Defined as EXIF orientation = "normal"
                    Windows.Foundation.PropertyType.UInt16
                ));

                await encoder.BitmapProperties.SetPropertiesAsync(propertySet);*/

                /*BitmapPropertySet properties = await encoder.BitmapProperties.GetPropertiesAsync(new []{ "/appext/Data" });
                properties = new BitmapPropertySet()
                {
                    {
                        "/appext/Application",
                        new BitmapTypedValue(Encoding.UTF8.GetBytes("NETSCAPE2.0"), Windows.Foundation.PropertyType.UInt8Array)
                    },
                    {
                        "/appext/Data",
                        new BitmapTypedValue(new byte[] { 3, 1, 0, 0, 0 }, Windows.Foundation.PropertyType.UInt8Array)
                    }
                };

                await encoder.BitmapProperties.SetPropertiesAsync(properties);*/

                var list = new List<string>();
                var bitmapPropertySet = await encoder.BitmapProperties.GetPropertiesAsync(list);

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
