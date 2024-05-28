using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Dispatching;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UnoFileDownloader.Presentation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            DataContextChanged += AboutPage_DataContextChanged;
            this.InitializeComponent();
            
            Loaded += AboutPage_Loaded;
        }

        private void AboutPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

        }

        private void AboutPage_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
