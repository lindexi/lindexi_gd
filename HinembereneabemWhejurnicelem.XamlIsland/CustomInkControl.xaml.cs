using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace HinembereneabemWhejurnicelem.XamlIsland
{
    public sealed partial class CustomInkControl : UserControl
    {
        public CustomInkControl()
        {
            this.InitializeComponent();
        }

        private void InkCanvas_OnLoaded(object sender, RoutedEventArgs e)
        {
            InkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse| CoreInputDeviceTypes.Touch;
        }
    }
}
