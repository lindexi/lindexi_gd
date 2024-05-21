using System.Diagnostics;
using Uno.Logging;

namespace UnoFileDownloader.Presentation
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            DataContextChanged += MainPage_DataContextChanged;
        }

        public BindableMainModel ViewModel => (BindableMainModel) DataContext;

        private void MainPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is BindableMainModel model)
            {
                model.DownloadFileInfoViewList.CollectionChanged += DownloadFileInfoViewList_CollectionChanged;
                UpdateTaskListNoItemsTextBlock();
            }
            else
            {
#if DEBUG
                // 谁，是谁，乱改 DataContext 的类型！
                // 在 WinUI 项目里面可能进入多次，这是符合预期的，只需要最后一次的 DataContext 是正确的就行了
                Debugger.Break();
#endif
            }
        }

        private void DownloadFileInfoViewList_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateTaskListNoItemsTextBlock();
        }

        /// <summary>
        /// 更新任务列表的“没有任务”文本块的可见性。
        /// </summary>
        private void UpdateTaskListNoItemsTextBlock()
        {
            TaskListNoItemsTextBlock.Visibility =
                // 如果下载列表为空，就显示“没有任务”文本块
                ViewModel.DownloadFileInfoViewList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DownloadItemOpenFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var fileInfo = (DownloadFileInfo) button.DataContext;

            if (!File.Exists(fileInfo.FilePath))
            {
                // 文件已经被删除了
                return;
            }

            try
            {
                // 先实现 Windows 下的打开文件功能。
                Process.Start(new ProcessStartInfo(fileInfo.FilePath)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                // 忽略吧，可能是需要管理员权限，但是用户取消了
                this.Log().LogWarning(exception, "打开文件失败");
            }
        }

        private void DownloadItemOpenContainFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var fileInfo = (DownloadFileInfo) button.DataContext;

            // 先实现 Windows 下的打开文件夹功能。
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fileInfo.FilePath}\"");
        }
    }
}
