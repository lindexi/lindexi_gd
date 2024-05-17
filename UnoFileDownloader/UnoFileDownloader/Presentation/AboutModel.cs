using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;

using UnoFileDownloader.Utils;

namespace UnoFileDownloader.Presentation
{
    public partial record AboutModel : INotifyPropertyChanged
    {
        private string _appInfo;

        public AboutModel(INavigator navigator, IDispatcherQueueProvider dispatcherQueueProvider, IStringLocalizer localizer)
        {
           Navigator = navigator;
           DispatcherQueueProvider = dispatcherQueueProvider;
           Localizer = localizer;
            Dispatcher = dispatcherQueueProvider.Dispatcher;

            var assemblyVersionAttribute = typeof(App).Assembly.GetAssemblyAttribute<AssemblyVersionAttribute>();
            var versionText = assemblyVersionAttribute?.Version ?? "1.0.0";
            _appInfo = $"{localizer["ApplicationName"]} {versionText}";
        }

        public string AppInfo
        {
            set
            {
                if (value == _appInfo)
                {
                    return;
                }

                _appInfo = value;
                OnPropertyChanged();
            }
            get => _appInfo;
        }

        public async Task CloseAbout()
        {
            var response = await Navigator.NavigateBackAsync(this);
            if (response is null)
            {
                response = await Navigator.NavigateViewModelAsync<NewTaskModel>(this);
            }
        }

        public void GotoGitHub()
        {
            Dispatcher.TryEnqueue(() =>
            {
                _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/dotnet-campus/dotnetCampus.FileDownloader"));
            });
        }

        protected DispatcherQueue Dispatcher { get; }
        public INavigator Navigator { get; init; }
        public IDispatcherQueueProvider DispatcherQueueProvider { get; init; }
        public IStringLocalizer Localizer { get; init; }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
