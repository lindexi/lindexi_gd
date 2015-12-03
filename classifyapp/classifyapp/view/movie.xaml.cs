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
    }
}
