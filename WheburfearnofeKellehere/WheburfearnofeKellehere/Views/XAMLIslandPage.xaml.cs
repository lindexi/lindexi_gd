using System.Windows.Controls;

using WheburfearnofeKellehere.ViewModels;

namespace WheburfearnofeKellehere.Views
{
    public partial class XAMLIslandPage : Page
    {
        public XAMLIslandPage(XAMLIslandViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
