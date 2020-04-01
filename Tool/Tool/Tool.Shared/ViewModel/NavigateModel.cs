using Tool.Shared.Framework;

namespace Tool.Shared.ViewModel
{
    public class NavigateModel : IViewModel, INavigateViewModelSensitivity
    {
        public INavigateViewModel NavigateViewModel { set; get; }

        public void OnNavigatedTo(object parameter)
        {

        }
    }
}