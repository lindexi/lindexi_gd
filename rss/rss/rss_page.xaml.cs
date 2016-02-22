using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using rss.ViewModel;

namespace rss
{
    public partial class rss_page : Page
    {
        public rss_page()
        {
            InitializeComponent();
        }

        private rssstr view;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            view = e.Parameter as rssstr;
            base.OnNavigatedTo(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var frame = Window.Current.Content as Frame;
            frame?.Navigate(typeof (MainPage));
        }
    }
}