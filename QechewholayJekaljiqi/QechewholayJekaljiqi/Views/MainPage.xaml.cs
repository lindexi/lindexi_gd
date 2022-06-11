using Microsoft.UI.Xaml.Controls;

using QechewholayJekaljiqi.ViewModels;

namespace QechewholayJekaljiqi.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            ViewModel = App.GetService<MainViewModel>();
            InitializeComponent();
        }
    }
}
