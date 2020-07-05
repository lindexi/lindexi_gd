using System.IO;
using System.Net;
using System.Threading.Tasks;

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

        public async Task DownloadFile()
        {
            var url = Url;

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            using var response = await webRequest.GetResponseAsync();
            var contentLength = response.ContentLength;

            var segmentManager = new SegmentManager(contentLength);

            var downloadSegment = segmentManager.GetNewDownloadSegment();

            var responseStream = response.GetResponseStream();

            DownLoad(downloadSegment, responseStream);


            await TryDownloadLast(url, contentLength, segmentManager);
        }

        private static async Task<bool> TryDownloadLast(string url, long contentLength, SegmentManager segmentManager)
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
                segmentManager.RegisterDownloadSegment(downloadSegment);

                DownLoad(downloadSegment, responseLast.GetResponseStream());

                return true;
            }

            return false;
        }

        private static void DownLoad(DownloadSegment downloadSegment, Stream responseStream)
        {

        }
    }
}