using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using rss.ViewModel;
using Windows.UI.Xaml.Navigation;

namespace rss
{
    public partial class rss_page : Page
    {
        private rssstr view;
        public rss_page()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            view = e.Parameter as rssstr;
            base.OnNavigatedTo(e);
        }

        private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame frame = Windows.UI.Xaml.Window.Current.Content as Frame;
            frame.Navigate(typeof(MainPage));
        }
    }
}
