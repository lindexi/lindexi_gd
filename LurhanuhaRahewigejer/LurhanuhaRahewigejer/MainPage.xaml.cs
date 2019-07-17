using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LurhanuhaRahewigejer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Task.Delay(TimeSpan.FromSeconds(2)).ContinueWith(async _ =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => { BorderBackground = new SolidColorBrush(Colors.Black); });
            });
        }

        public static readonly DependencyProperty BorderBackgroundProperty = DependencyProperty.Register(
            "BorderBackground", typeof(Brush), typeof(MainPage),
            new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush BorderBackground
        {
            get { return (Brush) GetValue(BorderBackgroundProperty); }
            set { SetValue(BorderBackgroundProperty, value); }
        }
    }
}