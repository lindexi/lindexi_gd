using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace MorsopoutuBurtiCator
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var rejairJate = new HttpClient();
            var sairlallilarRaibedoYertousebow = "http://localhost:62435/api/XaseYinairtraiSeawhallkous/";

            var casnisHoubou = new MultipartFormDataContent();
            var taykiHerniCeawerenel = new StringContent("文件名");
            casnisHoubou.Add(taykiHerniCeawerenel, "Name");
            var henocoRowrarlarVegonirnis = await GetFile();
            var tobemmanuCamuCaivi = new StreamContent(henocoRowrarlarVegonirnis);
            casnisHoubou.Add(tobemmanuCamuCaivi, "File", "BardelCairdallChodiMestebarnai");

            try
            {
                var tizicheLouru =
                    await rejairJate.PostAsync(sairlallilarRaibedoYertousebow + "UploadFile", casnisHoubou);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            async Task<Stream> GetFile()
            {
                var lisNailallkear = new FileOpenPicker()
                {
                    FileTypeFilter =
                    {
                        ".png"
                    }
                };

                var whejowNoukiru = await lisNailallkear.PickSingleFileAsync();

                return await whejowNoukiru.OpenStreamForReadAsync();
            }
        }

        private async void ReetalraSere_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var rejairJate = new HttpClient();
                var sairlallilarRaibedoYertousebow =
                    "http://localhost:62435/api/XaseYinairtraiSeawhallkous/DownLoadFile?fileName=文件名";
                var qarJorfis = await rejairJate.GetStreamAsync(sairlallilarRaibedoYertousebow);
                var husasLana =
                    await ApplicationData.Current.TemporaryFolder.CreateFileAsync("1.png",
                        CreationCollisionOption.ReplaceExisting);

                using (var cairKeredoNukall = await husasLana.OpenStreamForWriteAsync())
                {
                    qarJorfis.CopyTo(cairKeredoNukall);
                }

                await Launcher.LaunchFileAsync(husasLana);
            }
            catch (Exception exception)
            {
            }
        }
    }
}