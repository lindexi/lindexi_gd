using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
using Tool.Shared.Model;
using Tool.Shared.ViewModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Tool.Shared.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NavigatePage : Page
    {
        public NavigatePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = (NavigateModel) e.Parameter;
            base.OnNavigatedTo(e);
        }

        public NavigateModel ViewModel { set; get; }

        public ObservableCollection<PageModel> PageList { get; } = new ObservableCollection<PageModel>()
        {
            new PageModel()
            {
                Name = "Json 属性大小写转换"
            },
            new PageModel()
            {
                Name = "PPT 单位转换"
            },
        };

        private void Grid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                var pageModel = (PageModel)e.AddedItems[0];

            }
        }
    }
}
