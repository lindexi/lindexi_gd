using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace FileDownloader
{
    /// <summary>
    /// 文件分段管理
    /// </summary>
    public class SegmentManager
    {
        /// <summary>
        /// 创建文件分段管理
        /// </summary>
        /// <param name="fileLength">文件长度</param>
        public SegmentManager(long fileLength)
        {
            FileLength = fileLength;
        }

        public long FileLength { get; }

        public void RegisterCurrentDownload(long startPoint, long downloadLength)
        {
            // 不需要的代码
        }

        /// <summary>
        /// 创建一个新的分段用于下载
        /// </summary>
        public DownloadSegment GetNewDownloadSegment()
        {
            lock (_locker)
            {
                var downloadSegment = NewDownloadSegment();

                RegisterDownloadSegment(downloadSegment);

                return downloadSegment;
            }
        }

        private DownloadSegment NewDownloadSegment()
        {
            if (DownloadSegmentList.Count == 0)
            {
                return new DownloadSegment(startPoint: 0, requirementDownloadPoint: FileLength);
            }

            // 此时需要拿到当前最大的空段是哪一段

            long emptySegmentLength = 0;
            var previousSegmentIndex = -1;

            for (var i = 0; i < DownloadSegmentList.Count - 1; i++)
            {
                var segment = DownloadSegmentList[i];
                var nextSegment = DownloadSegmentList[i + 1];

                var emptyLength = nextSegment.StartPoint - segment.CurrentDownloadPoint;
                if (emptyLength > emptySegmentLength)
                {
                    emptySegmentLength = emptyLength;
                    previousSegmentIndex = i;
                }
            }

            // 最后一段
            var lastDownloadSegmentIndex = DownloadSegmentList.Count - 1;
            var lastDownloadSegment = DownloadSegmentList[lastDownloadSegmentIndex];
            var lastDownloadSegmentEmptyLength = FileLength - lastDownloadSegment.CurrentDownloadPoint;
            if (lastDownloadSegmentEmptyLength > emptySegmentLength)
            {
                emptySegmentLength = lastDownloadSegmentEmptyLength;
                previousSegmentIndex = lastDownloadSegmentIndex;
            }

            if (previousSegmentIndex >= 0)
            {
                long requirementDownloadPoint;

                var previousDownloadSegment = DownloadSegmentList[previousSegmentIndex];
                long currentDownloadPoint = previousDownloadSegment.CurrentDownloadPoint;

                if (previousSegmentIndex == lastDownloadSegmentIndex)
                {
                    requirementDownloadPoint = FileLength;
                }
                else
                {
                    var nextDownloadSegment = DownloadSegmentList[previousSegmentIndex + 1];
                    requirementDownloadPoint = nextDownloadSegment.StartPoint;
                }

                var length = emptySegmentLength;
                var center = length / 2 + currentDownloadPoint;

                previousDownloadSegment.RequirementDownloadPoint = center;
                return new DownloadSegment(center, requirementDownloadPoint);
            }

            return null;
        }

        public void RegisterDownloadSegment(DownloadSegment downloadSegment)
        {
            lock (_locker)
            {
                // 找到顺序
                var n = DownloadSegmentList.FindIndex(temp => temp.StartPoint > downloadSegment.StartPoint);
                if (n < 0)
                {
                    DownloadSegmentList.Add(downloadSegment);
                }
                else
                {
                    DownloadSegmentList.Insert(n, downloadSegment);
                }

                downloadSegment.SegmentManager = this;
            }
        }

        public List<DownloadSegment> DownloadSegmentList { get; } = new List<DownloadSegment>();
        private readonly object _locker = new object();


        readonly struct Segment
        {
            public Segment(long startPoint, long length)
            {
                StartPoint = startPoint;
                Length = length;
            }

            public long StartPoint { get; }
            public long Length { get; }
        }
    }

    //internal static class EmptySegmentCalculator
    //{
    //    public void 
    //}
}