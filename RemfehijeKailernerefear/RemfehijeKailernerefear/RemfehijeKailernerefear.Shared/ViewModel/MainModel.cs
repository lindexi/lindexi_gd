using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Appointments;
using Tool.Shared.Framework;
using Tool.Shared.Model;

namespace Tool.Shared.ViewModel
{
    public class MainModel : INavigateViewModel, IViewModel
    {
        public event EventHandler<NavigationPage> OnNavigate;

        public void Navigate(string name, object parameter)
        {
            var viewModel = ViewModelPageBind.CreateViewModel(name);
            viewModel.OnNavigatedTo(parameter);

            if (viewModel is INavigateViewModelSensitivity navigateViewModelSensitivity)
            {
                navigateViewModelSensitivity.NavigateViewModel = this;
            }

            OnNavigate?.Invoke(this,new NavigationPage()
            {
                PageName = name,
                Parameter = new NavigationModel()
                {
                    ViewModel = viewModel
                }
            });
        }

        public void OnNavigatedTo(object parameter)
        {
            ViewModelPageBind = (ViewModelPageBind)parameter;

            Navigate("NavigatePage", this);
        }

        public ViewModelPageBind ViewModelPageBind { private set; get; }
    }
}
