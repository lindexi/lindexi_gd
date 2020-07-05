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
                if (DownloadSegmentList.Count == 0)
                {
                    return new DownloadSegment(0)
                    {
                        SegmentManager = this,
                        RequirementDownloadLength = FileLength
                    };
                }
                else if (DownloadSegmentList.Count == 1)
                {
                    // 只有一段的时候，假定这一段就是开始
                    var firstDownloadSegment = DownloadSegmentList[0];
                    Debug.Assert(firstDownloadSegment.StartPoint == 0);

                    // 当前下载到的地方
                    var currentDownloadPoint = firstDownloadSegment.CurrentDownloadPoint;
                    // 找到中间的下载
                    var center = FileLength - currentDownloadPoint;
                    // 更新当前第一个的下载范围

                }
            }

            return null;
        }

        public void RegisterDownloadSegment(DownloadSegment downloadSegment)
        {
            lock (_locker)
            {
                DownloadSegmentList.Add(downloadSegment);
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
}