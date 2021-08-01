using System.Windows.Controls;

using WheburfearnofeKellehere.ViewModels;

namespace WheburfearnofeKellehere.Views
{
    public partial class ListDetailsPage : Page
    {
        public ListDetailsPage(ListDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
