using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.Extensions.Logging;

namespace FileDownloader
{
    public class SegmentFileDownloader
    {
        private readonly ILogger<SegmentFileDownloader> _logger;

        public SegmentFileDownloader(string url, FileInfo file, ILogger<SegmentFileDownloader> logger)
        {
            _logger = logger;
            Url = url;
            File = file;

            logger.BeginScope("Url={url} File={file}", url, file);
        }

        public string Url { get; }

        public FileInfo File { get; }

        private RandomFileWriter FileWriter { set; get; }

        private FileStream FileStream { set; get; }

        private TaskCompletionSource<bool> FileDownloadTask { set; get; } = new TaskCompletionSource<bool>();

        private SegmentManager SegmentManager { set; get; }

        /// <summary>
        /// 获取整个下载的长度
        /// </summary>
        /// <returns></returns>
        private async Task<(WebResponse response, long contentLength)> GetContentLength()
        {
            _logger.LogInformation($"开始获取整个下载长度");

            // 如果用户没有说停下，那么不断下载

            for (int i = 0; true; i++)
            {
                try
                {
                    var url = Url;
                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                    webRequest.Method = "GET";
                    var response = await webRequest.GetResponseAsync();
                    var contentLength = response.ContentLength;

                    return (response, contentLength);
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"第{i}次获取长度失败 {e}");
                }

                // 后续需要配置不断下降时间
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        public async Task DownloadFile()
        {
            _logger.LogInformation($"Start download Url={Url} File={File.FullName}");

            (WebResponse response, long contentLength) = await GetContentLength();

            FileStream = File.Create();
            FileStream.SetLength(contentLength);
            FileWriter = new RandomFileWriter(FileStream);

            SegmentManager = new SegmentManager(contentLength);

            var downloadSegment = SegmentManager.GetNewDownloadSegment();

            // 下载第一段
            Download(response, downloadSegment);

            var supportSegment = await TryDownloadLast(contentLength);

            var threadCount = 1;

            if (supportSegment)
            {
                // 多创建几个线程下载
                for (int i = 0; i < 2; i++)
                {
                    Download(SegmentManager.GetNewDownloadSegment());
                }

                threadCount = 10;
            }

            for (int i = 0; i < threadCount; i++)
            {
                _ = Task.Run(DownloadTask);
            }

            await FileDownloadTask.Task;
        }

        private async Task<WebResponse> CreateDownloadTask(DownloadSegment downloadSegment)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url);
            webRequest.Method = "GET";

            // 为什么不使用 StartPoint 而是使用 CurrentDownloadPoint 是因为需要处理重试
            webRequest.AddRange(downloadSegment.CurrentDownloadPoint, downloadSegment.RequirementDownloadPoint);

            var response = await webRequest.GetResponseAsync();
            return response;
        }

        private async Task DownloadTask()
        {
            while (SegmentManager.IsFinished())
            {
                var data = await DownloadDataList.DequeueAsync();

                var downloadSegment = data.DownloadSegment;
                using var response = data.WebResponse ?? await CreateDownloadTask(downloadSegment);

                try
                {
                    await using var responseStream = response.GetResponseStream();
                    const int length = 1024;
                    var buffer = SharedArrayPool.Rent(length);
                    int n = 0;
                    Debug.Assert(responseStream != null, nameof(responseStream) + " != null");
                    while ((n = await responseStream.ReadAsync(buffer, 0, length)) > 0)
                    {
                        FileWriter.WriteAsync(downloadSegment.CurrentDownloadPoint, buffer, n);

                        downloadSegment.DownloadedLength += n;

                        if (downloadSegment.Finished)
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"Download {downloadSegment.StartPoint}-{downloadSegment.RequirementDownloadPoint} error {e}");

                    // 下载失败了，那么放回去继续下载
                    Download(downloadSegment);
                }
            }

            await FinishDownload();
        }

        private void Download(WebResponse webResponse, DownloadSegment downloadSegment)
        {
            DownloadDataList.Enqueue(new DownloadData(webResponse, downloadSegment));
        }

        private void Download(DownloadSegment downloadSegment)
            => Download(null, downloadSegment);

        private AsyncQueue<DownloadData> DownloadDataList { get; } = new AsyncQueue<DownloadData>();


        private class DownloadData
        {
            public DownloadData(WebResponse webResponse, DownloadSegment downloadSegment)
            {
                WebResponse = webResponse;
                DownloadSegment = downloadSegment;
            }

            public WebResponse WebResponse { get; }

            public DownloadSegment DownloadSegment { get; }
        }

        private async Task FinishDownload()
        {
            await FileWriter.DisposeAsync();
            await FileStream.DisposeAsync();

            FileDownloadTask.SetResult(true);
        }

        private async Task<bool> TryDownloadLast(long contentLength)
        {
            string url = Url;
            // 尝试下载后部分，如果可以下载后续的 100 个字节，那么这个链接支持分段下载
            const int downloadLength = 100;
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            var startPoint = contentLength - downloadLength;
            webRequest.AddRange(startPoint, contentLength);
            var responseLast = await webRequest.GetResponseAsync();

            if (responseLast.ContentLength == downloadLength)
            {
                var downloadSegment = new DownloadSegment(startPoint, contentLength);
                SegmentManager.RegisterDownloadSegment(downloadSegment);

                Download(responseLast, downloadSegment);

                return true;
            }

            return false;
        }
    }
}