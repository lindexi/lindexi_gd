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
using classifyapp.ViewModel;


namespace classifyapp.view
{
    public sealed partial class movie:Page
    {
        public movie()
        {
            InitializeComponent();
        }

        private void souhu_Click(object sender , RoutedEventArgs e)
        {            
            string ProductId = "9wzdncrfhvq0";
            _model.windowsapp(ProductId);
        }

        private model _model
        {
            set
            {
                value = null;
            }
            get
            {
                return model.cmodel();
            }
        }

        private void blibli_Click(object sender , RoutedEventArgs e)
        {
            //https://www.microsoft.com/zh-cn/store/apps/%E5%93%94%E5%93%A9%E5%93%94%E5%93%A9%E5%8A%A8%E7%94%BB/9wzdncrfj28k
            string ProductId = "9wzdncrfj28k";
            _model.windowsapp(ProductId);
        }

        private void manguo_Click(object sender , RoutedEventArgs e)
        {
            string ProductId = "9nblggh62k52";
            _model.windowsapp(ProductId);
        }

        private void youku_Click(object sender , RoutedEventArgs e)
        {
            string ProductId = "9wzdncrdv0bk";
            _model.windowsapp(ProductId);
        }

        private void baofengyingyin_Click(object sender , RoutedEventArgs e)
        {
            string ProductId = "9wzdncrfhv95";
            _model.windowsapp(ProductId);
        }

        private void qiyi_Click(object sender , RoutedEventArgs e)
        {
            string ProductId = "9wzdncrfj15n";
            _model.windowsapp(ProductId);
        }

        private void xunlei_Click(object sender , RoutedEventArgs e)
        {
            string ProductId = "9wzdncrfj3wx";
            _model.windowsapp(ProductId);
        }


    }
}
