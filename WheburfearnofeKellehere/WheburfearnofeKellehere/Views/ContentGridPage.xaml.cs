using System.Windows.Controls;

using WheburfearnofeKellehere.ViewModels;

namespace WheburfearnofeKellehere.Views
{
    public partial class ContentGridPage : Page
    {
        public ContentGridPage(ContentGridViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
