using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

using WheburfearnofeKellehere.Contracts.Services;
using WheburfearnofeKellehere.Core.Contracts.Services;
using WheburfearnofeKellehere.Core.Helpers;
using WheburfearnofeKellehere.Properties;

namespace WheburfearnofeKellehere.ViewModels
{
    // You can show pages in different ways (update main view, navigate, right pane, new windows or dialog)
    // using the NavigationService, RightPaneService and WindowManagerService.
    // Read more about MenuBar project type here:
    // https://github.com/Microsoft/WindowsTemplateStudio/blob/release/docs/WPF/projectTypes/menubar.md
    public class ShellViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IRightPaneService _rightPaneService;
        private readonly IIdentityService _identityService;

        private bool _isLoggedIn;
        private bool _isAuthorized;

        private RelayCommand _goBackCommand;
        private ICommand _menuFileSettingsCommand;
        private ICommand _menuViewsXAMLIslandCommand;
        private ICommand _menuViewsDataGridCommand;
        private ICommand _menuViewsContentGridCommand;
        private ICommand _menuViewsListDetailsCommand;
        private ICommand _menuViewsMainCommand;
        private ICommand _menuFileExitCommand;
        private ICommand _loadedCommand;
        private ICommand _unloadedCommand;

        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set { SetProperty(ref _isLoggedIn, value); }
        }

        public bool IsAuthorized
        {
            get { return _isAuthorized; }
            set { SetProperty(ref _isAuthorized, value); }
        }

        public RelayCommand GoBackCommand => _goBackCommand ?? (_goBackCommand = new RelayCommand(OnGoBack, CanGoBack));

        public ICommand MenuFileSettingsCommand => _menuFileSettingsCommand ?? (_menuFileSettingsCommand = new RelayCommand(OnMenuFileSettings));

        public ICommand MenuFileExitCommand => _menuFileExitCommand ?? (_menuFileExitCommand = new RelayCommand(OnMenuFileExit));

        public ICommand LoadedCommand => _loadedCommand ?? (_loadedCommand = new RelayCommand(OnLoaded));

        public ICommand UnloadedCommand => _unloadedCommand ?? (_unloadedCommand = new RelayCommand(OnUnloaded));

        public ShellViewModel(INavigationService navigationService, IRightPaneService rightPaneService, IIdentityService identityService)
        {
            _navigationService = navigationService;
            _rightPaneService = rightPaneService;
            _identityService = identityService;
            _identityService.LoggedIn += OnLoggedIn;
            _identityService.LoggedOut += OnLoggedOut;
        }

        private void OnLoaded()
        {
            _navigationService.Navigated += OnNavigated;
            IsLoggedIn = _identityService.IsLoggedIn();
            IsAuthorized = IsLoggedIn && _identityService.IsAuthorized();
        }

        private void OnLoggedIn(object sender, EventArgs e)
        {
            IsLoggedIn = true;
            IsAuthorized = IsLoggedIn && _identityService.IsAuthorized();
        }

        private void OnLoggedOut(object sender, EventArgs e)
        {
            IsLoggedIn = false;
            IsAuthorized = false;
            _navigationService.CleanNavigation();
            _navigationService.NavigateTo(typeof(MainViewModel).FullName, null, true);
        }

        private void OnUnloaded()
        {
            _rightPaneService.CleanUp();
            _navigationService.Navigated -= OnNavigated;
        }

        private bool CanGoBack()
            => _navigationService.CanGoBack;

        private void OnGoBack()
            => _navigationService.GoBack();

        private void OnNavigated(object sender, string viewModelName)
        {
            GoBackCommand.NotifyCanExecuteChanged();
        }

        private void OnMenuFileExit()
            => Application.Current.Shutdown();

        public ICommand MenuViewsMainCommand => _menuViewsMainCommand ?? (_menuViewsMainCommand = new RelayCommand(OnMenuViewsMain));

        private void OnMenuViewsMain()
            => _navigationService.NavigateTo(typeof(MainViewModel).FullName, null, true);

        public ICommand MenuViewsListDetailsCommand => _menuViewsListDetailsCommand ?? (_menuViewsListDetailsCommand = new RelayCommand(OnMenuViewsListDetails));

        private void OnMenuViewsListDetails()
            => _navigationService.NavigateTo(typeof(ListDetailsViewModel).FullName, null, true);

        public ICommand MenuViewsContentGridCommand => _menuViewsContentGridCommand ?? (_menuViewsContentGridCommand = new RelayCommand(OnMenuViewsContentGrid));

        private void OnMenuViewsContentGrid()
            => _navigationService.NavigateTo(typeof(ContentGridViewModel).FullName, null, true);

        public ICommand MenuViewsDataGridCommand => _menuViewsDataGridCommand ?? (_menuViewsDataGridCommand = new RelayCommand(OnMenuViewsDataGrid));

        private void OnMenuViewsDataGrid()
            => _navigationService.NavigateTo(typeof(DataGridViewModel).FullName, null, true);

        public ICommand MenuViewsXAMLIslandCommand => _menuViewsXAMLIslandCommand ?? (_menuViewsXAMLIslandCommand = new RelayCommand(OnMenuViewsXAMLIsland));

        private void OnMenuViewsXAMLIsland()
            => _navigationService.NavigateTo(typeof(XAMLIslandViewModel).FullName, null, true);

        private void OnMenuFileSettings()
            => _rightPaneService.OpenInRightPane(typeof(SettingsViewModel).FullName);
    }
}
