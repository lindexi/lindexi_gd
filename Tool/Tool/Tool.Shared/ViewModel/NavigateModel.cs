using System.Collections.ObjectModel;
using Tool.Shared.Framework;
using Tool.Shared.Model;
using Uno.Extensions.Specialized;

namespace Tool.Shared.ViewModel
{
    public class NavigateModel : IViewModel, INavigateViewModelSensitivity
    {
        public INavigateViewModel NavigateViewModel { set; get; }

        public void OnNavigatedTo(object parameter)
        {
            NavigateViewModel = (INavigateViewModel)parameter;

            if (parameter is MainModel mainModel)
            {
                PageList.Clear();
                foreach (var temp in mainModel.ViewModelPageBind.PageModelList)
                {
                    if (!string.IsNullOrEmpty(temp.Describe))
                    {
                        PageList.Add(temp);
                    }
                }
            }
        }

        public void Navigate(PageModel pageModel)
        {
            NavigateViewModel.Navigate(pageModel.Name, null);
        }

        public ObservableCollection<PageModel> PageList { get; } = new ObservableCollection<PageModel>();
    }
}