using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using dotnetCampus.FileDownloader;
using UnoFileDownloader.Business;
using UnoFileDownloader.Utils;

namespace UnoFileDownloader.Presentation
{
    public partial record NewTaskModel(INavigator Navigator, DownloadFileListManager DownloadFileListManager,
        ILogger<SegmentFileDownloader> Logger)
    {
        public string? DownloadSource { set; get; }

#if DEBUG
            = "http://127.0.0.1:56611/FileDownloader.exe";
#endif

        public string? FileName { set; get; }

        public async Task StartDownloadAsync()
        {
            if (string.IsNullOrEmpty(DownloadSource))
            {
                return;
            }

            var filePath = FileName;

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.GetFileName(DownloadSource);
            }

            if (!Path.IsPathFullyQualified(filePath))
            {
                var downloadFolder = GetDownloadsWellKnownFolder();
                if (!string.IsNullOrEmpty(downloadFolder))
                {
                    filePath = Path.Join(downloadFolder, filePath);
                }
            }

            filePath = Path.GetFullPath(filePath);

            var downloadFileInfo = new DownloadFileInfo()
            {
                AddedTime = DateTime.Now.ToString(format: null),
                DownloadUrl = DownloadSource,
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
            };
            await DownloadFileListManager.AddDownloadFileAsync(downloadFileInfo);

            // 后台慢慢下载
            _ = Task.Run(async () =>
            {
                // 用于让界面切换到下载列表
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                using var progress = new FileDownloadSpeedProgress();
                progress.ProgressChanged += (sender, downloadProgress) =>
                {
                    downloadFileInfo.DownloadSpeed = downloadProgress.DownloadSpeed;
                    downloadFileInfo.FileSize = downloadProgress.FileSize;
                    downloadFileInfo.DownloadProcess = downloadProgress.DownloadProcess;
                };
                // 断点续传文件
                var breakPointResumptionTransmissionRecordFile = filePath + ".dat";
                using var httpClient = new HttpClient();
                var segmentFileDownloader = new SegmentFileDownloaderByHttpClient(DownloadSource,
                    new FileInfo(filePath), httpClient, Logger, progress,
                    sharedArrayPool: SharedArrayPool, bufferLength: FileDownloaderSharedArrayPool.BufferLength,
                    breakpointResumptionTransmissionRecordFile: new FileInfo(
                        breakPointResumptionTransmissionRecordFile));

                bool success = false;
                try
                {
                    await segmentFileDownloader.DownloadFileAsync();
                    success = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Debugger.Launch();
                    Debugger.Break();
                }

                // 下载完成逻辑
                progress.Stop();

                if (success)
                {
                    downloadFileInfo.DownloadSpeed = "";
                    downloadFileInfo.DownloadProcess = "完成";
                }
                else
                {
                    downloadFileInfo.DownloadSpeed = "";
                    downloadFileInfo.DownloadProcess = "失败";
                }

                downloadFileInfo.IsFinished = success;

                // 完成之后，保存列表信息一下
                await DownloadFileListManager.SaveAsync();
            });

            // 似乎 NavigateViewModelAsync 跳转不回去，不知道为什么
            // await Navigator.NavigateViewModelAsync<MainModel>(this)
            var response = await Navigator.NavigateBackAsync(this);
            GC.KeepAlive(response);
        }

        public async Task CloseNewTask()
        {
            var response = await Navigator.NavigateBackAsync(this);
            if (response is null)
            {
                response = await Navigator.NavigateViewModelAsync<MainModel>(this);
            }
        }

        private ISharedArrayPool SharedArrayPool { get; } = new FileDownloaderSharedArrayPool();

        /// <summary>
        /// Copy from https://github.com/AvaloniaUI/Avalonia/blob/0bf3ffc05b4ba6ad59de501099f9d9ba39cbd944/src/Avalonia.Base/Platform/Storage/FileIO/BclStorageProvider.cs#L102
        /// </summary>
        /// <returns></returns>
        private static string? GetDownloadsWellKnownFolder()
        {
            if (OperatingSystem.IsWindows())
            {
                return Environment.OSVersion.Version.Major < 6 ? null :
                    SHGetKnownFolderPath(s_folderDownloads, 0, IntPtr.Zero);
            }

            if (OperatingSystem.IsLinux())
            {
                var envDir = Environment.GetEnvironmentVariable("XDG_DOWNLOAD_DIR");
                if (envDir != null && Directory.Exists(envDir))
                {
                    return envDir;
                }
            }

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return "~/Downloads";
            }

            return null;
        }

        private static readonly Guid s_folderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, int flags, IntPtr token);
    }
}
