using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using lindexi.MVVM.Framework.ViewModel;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace NubevearKenireno
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            ViewModel = (ViewModel) DataContext;
            ViewModel.OnNavigatedTo(this, null);
        }

        private ViewModel ViewModel { set; get; }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Send();
        }

        private async void Send()
        {
            if (string.IsNullOrEmpty(ViewModel.Name) || string.IsNullOrEmpty(ViewModel.Code) ||
                string.IsNullOrEmpty(ViewModel.Text))
            {
                return;
            }

            var qpush = new Qpush(ViewModel.Name, ViewModel.Code);

            try
            {
                await qpush.PushMessageAsync(ViewModel.Text);
            }
            catch (HttpRequestException)
            {
            }

            ViewModel.Text = "";
        }

        private void Text_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Send();
        }
    }

    public class ViewModel : NavigateViewModel
    {
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                Storage();
                OnPropertyChanged();
            }
        }

        public string Code
        {
            get => _code;
            set
            {
                if (value == _code) return;
                _code = value;
                Storage();
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public override void OnNavigatedFrom(object sender, object obj)
        {
            Storage();
            base.OnNavigatedFrom(sender, obj);
        }

        /// <inheritdoc />
        public override void OnNavigatedTo(object sender, object obj)
        {
            Read();
            base.OnNavigatedTo(sender, obj);
        }

        private string _code;
        private string _name;
        private string _text;

        private async void Read()
        {
            try
            {
                //var fileInfo = new FileInfo(File);
                //if (fileInfo.Exists)
                //{
                //    using (var stream = fileInfo.OpenText())
                //    {
                //        var name = stream.ReadLine();
                //        var code = stream.ReadLine();

                //        Name = name;
                //        Code = code;
                //    }
                //}

                try
                {
                    var file = await ApplicationData.Current.RoamingFolder.GetFileAsync(File);
                    using (var stream = await file.OpenReadAsync())
                    {
                        var streamReader = new StreamReader(stream.AsStreamForRead());
                        using (streamReader)
                        {
                            var name = streamReader.ReadLine();
                            var code = streamReader.ReadLine();

                            Name = name;
                            Code = code;
                        }
                    }
                }
                catch (FileNotFoundException )
                {
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async void Storage()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Code))
            {
                return;
            }

            try
            {
                var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(File,
                    CreationCollisionOption.ReplaceExisting);
                using (StorageStreamTransaction transaction = await file.OpenTransactedWriteAsync())
                {
                    using (DataWriter dataWriter = new DataWriter(transaction.Stream))
                    {
                        dataWriter.WriteString($"{Name}\r\n{Code}");
                        transaction.Stream.Size = await dataWriter.StoreAsync(); // reset stream size to override the file
                        await transaction.CommitAsync();
                    }
                }
               
            }
            catch (Exception )
            {
                
            }
        }

        private const string File = "file.txt";
    }

    /// <summary>
    /// QPush 快推 从电脑到手机最方便的文字推送工具
    /// </summary>
    public class Qpush
    {
        public Qpush(string name, string code)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (code == null) throw new ArgumentNullException(nameof(code));

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name 不能为空");
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException("code 不能为空");
            }

            Name = name;
            Code = code;
        }

        /// <summary>
        /// 推名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 推码
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// 推送信息
        /// </summary>
        public async Task<string> PushMessageAsync(string str)
        {
            const string url = "https://qpush.me/pusher/push_site/";

            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");

            HttpContent content =
                new StringContent(
                    $"name={Uri.EscapeUriString(Name)}&code={Uri.EscapeUriString(Code)}&sig=&cache=false&msg%5Btext%5D={Uri.EscapeUriString(str)}",
                    Encoding.UTF8, "application/x-www-form-urlencoded");
            var code = await (await httpClient.PostAsync(url, content)).Content.ReadAsStringAsync();

            return code;
        }
    }
}
