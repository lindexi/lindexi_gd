using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
using lindexi.src;

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

        private void Read()
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Storage()
        {
            //if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Code))
            //{
            //    return;
            //}

            //var str = Name + "\n" + Code;
            //System.IO.File.WriteAllText(File, str, Encoding.UTF8);
        }

        private const string File = "file.txt";
    }
}
