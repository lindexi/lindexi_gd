using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace LawhallwheachalNakearjalle
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
    }

    public class Template1: Grid
    {

    }

    public class Template2 : Grid
    {

    }

    public class Test1
    {

    }

    public  class Test2
    {

    }

    public class ItemTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is Test1)
            {
                return Template1;
            }
            else if (item is Test2)
            {
                return Template2;
            }
            else
            {
                return base.SelectTemplateCore(item);
            }
        }

        public DataTemplate Template1 { get; set; }
        public DataTemplate Template2 { get; set; }
    }
}
