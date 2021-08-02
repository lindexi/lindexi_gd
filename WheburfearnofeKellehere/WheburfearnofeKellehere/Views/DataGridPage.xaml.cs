using System.Windows.Controls;

using WheburfearnofeKellehere.ViewModels;

namespace WheburfearnofeKellehere.Views
{
    public partial class DataGridPage : Page
    {
        public DataGridPage(DataGridViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
