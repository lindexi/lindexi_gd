using rss.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
namespace rss
{
    public class model
    {
        public model()
        {
            
        }

        public static model _model { set; get; }=new model();

        public bool rss_content
        {
            set
            {
                _rss_content = value;
            }
            get
            {
                return _rss_content;
            }
        }

        private bool _rss_content;
    }
}