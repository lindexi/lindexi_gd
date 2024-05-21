using UnoFileDownloader.Business;

namespace UnoFileDownloader.Presentation
{
    public class ShellModel
    {
        private readonly INavigator _navigator;
        private readonly DownloadFileListManager _downloadFileListManager;

        public ShellModel(INavigator navigator, DownloadFileListManager downloadFileListManager)
        {
            _navigator = navigator;
            _downloadFileListManager = downloadFileListManager;
            _ = Start();
        }

        public async Task Start()
        {
            await _downloadFileListManager.InitAsync();
            await _navigator.NavigateViewModelAsync<MainModel>(this);
        }
    }
}
