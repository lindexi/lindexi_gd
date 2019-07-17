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

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace UhajoloraKdzvhy
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

        public static readonly DependencyProperty CursorProperty = DependencyProperty.Register(
            "Cursor", typeof(string), typeof(MainPage), new PropertyMetadata(default(string), (s, e) =>
            {
                if (e.NewValue != null)
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor((CoreCursorType) Enum.Parse(typeof(CoreCursorType), (string) e.NewValue), 0);
                }
                else
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(CoreCursorType.Arrow, 0);

                }
            }));

        public string Cursor
        {
            get { return (string) GetValue(CursorProperty); }
            set { SetValue(CursorProperty, value); }
        }
    }
}
