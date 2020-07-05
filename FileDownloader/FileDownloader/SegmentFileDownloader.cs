using System.IO;
using System.Net;
using System.Threading.Tasks;
using dotnetCampus.Threading;

namespace FileDownloader
{
    public class SegmentFileDownloader
    {
        public SegmentFileDownloader(string url, FileInfo file)
        {
            Url = url;
            File = file;
        }

        public string Url { get; }

        public FileInfo File { get; }

        private RandomFileWriter FileWriter { set; get; }

        private FileStream FileStream { set; get; }

        private TaskCompletionSource<bool> FileDownloadTask { set; get; } = new TaskCompletionSource<bool>();

        private SegmentManager SegmentManager { set; get; }

        public async Task DownloadFile()
        {
            var url = Url;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            using var response = await webRequest.GetResponseAsync();
            var contentLength = response.ContentLength;

            FileStream = File.Create();
            FileStream.SetLength(contentLength);
            FileWriter = new RandomFileWriter(FileStream);

            SegmentManager = new SegmentManager(contentLength);

            var downloadSegment = SegmentManager.GetNewDownloadSegment();

            // 下载第一段
            Download(response, downloadSegment);

            var supportSegment = await TryDownloadLast(url, contentLength);

            var threadCount = 1;

            if (supportSegment)
            {
                // 多创建几个线程下载
                for (int i = 0; i < 2; i++)
                {
                    await CreateDownloadTask();
                }

                threadCount = 10;
            }

            for (int i = 0; i < threadCount; i++)
            {
                _ = Task.Run(DownloadTask);
            }

            await FileDownloadTask.Task;
        }

        private async Task CreateDownloadTask()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url);
            webRequest.Method = "GET";

            var downloadSegment = SegmentManager.GetNewDownloadSegment();
            webRequest.AddRange(downloadSegment.StartPoint, downloadSegment.RequirementDownloadPoint);

            using var response = await webRequest.GetResponseAsync();
            Download(response, downloadSegment);
        }

        private async Task DownloadTask()
        {
            while (SegmentManager.IsFinished())
            {
                var data = await DownloadDataList.DequeueAsync();

                var dataDownloadSegment = data.DownloadSegment;

                await using var responseStream = data.WebResponse.GetResponseStream();
                const int length = 1024;
                var buffer = SharedArrayPool.Rent(length);
                int n = 0;
                while ((n = await responseStream.ReadAsync(buffer, 0, length)) > 0)
                {
                    FileWriter.WriteAsync(dataDownloadSegment.CurrentDownloadPoint, buffer, n);

                    dataDownloadSegment.DownloadedLength += n;

                    if (dataDownloadSegment.Finished)
                    {
                        break;
                    }
                }
            }
        }

        private void Download(WebResponse webResponse, DownloadSegment downloadSegment)
        {
            DownloadDataList.Enqueue(new DownloadData(webResponse, downloadSegment));
        }

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

        private async Task<bool> TryDownloadLast(string url, long contentLength)
        {
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