using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace produproperty
{
    public partial class  option
    {
        viewModel view;
        public option()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            view = e.Parameter as viewModel;
        }

        private void mainpage(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            frame.Navigate(typeof(MainPage), view);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            view.accessfolder();
        }
    }
}
