using System.Collections.ObjectModel;
using System.Xml.Linq;

using Uno.Extensions;

using UnoFileDownloader.Business;
using UnoFileDownloader.Utils;

namespace UnoFileDownloader.Presentation
{
    public partial record MainModel
    {
        private INavigator _navigator;
        private readonly DownloadFileListManager _downloadFileListManager;

        public MainModel
        (
            IStringLocalizer localizer,
            IOptions<AppConfig> appInfo,
            INavigator navigator,
            DownloadFileListManager downloadFileListManager
        )
        {
            _navigator = navigator;
            _downloadFileListManager = downloadFileListManager;

            UpdateDownloadFileInfoViewList();
            _downloadFileListManager.DownloadFileInfoList.CollectionChanged += DownloadFileInfoList_CollectionChanged;
        }

        private void DownloadFileInfoList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateDownloadFileInfoViewList();
        }

        public ObservableCollection<DownloadFileInfo> DownloadFileInfoViewList { get; } =
            new ObservableCollection<DownloadFileInfo>();

        public async Task GotToNewTask()
        {
            await _navigator.NavigateViewModelAsync<NewTaskModel>(this);
        }

        public async Task GoToAbout()
        {
            await _navigator.NavigateViewModelAsync<AboutModel>(this);
        }

        public async Task CleanDownloadList()
        {
            _downloadFileListManager.DownloadFileInfoList.Clear();
            await _downloadFileListManager.SaveAsync();
        }

        private void UpdateDownloadFileInfoViewList()
        {
            DownloadFileInfoViewList.Clear();
            DownloadFileInfoViewList.AddRange(_downloadFileListManager.DownloadFileInfoList);
        }
    }
}
