using System;
using System.Collections.Generic;

namespace FileDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    /// <summary>
    /// 文件分段管理
    /// </summary>
    public class SegmentManager
    {
        public long FileLength { get; }

        public void RegisterCurrentDownload(long startPoint, long downloadLength)
        {

        }

        public List<DownloadSegment> DownloadSegmentList { get; } = new List<DownloadSegment>();
    }

    /// <summary>
    /// 下载的段，这个段的内容和长度将会不断更改
    /// </summary>
    public class DownloadSegment
    {
        private long _startPoint;
        private long _requirementDownloadLength;
        private long _downloadedLength;

        public event EventHandler SegmentChanged;

        public long StartPoint
        {
            get => _startPoint;
        }

        /// <summary>
        /// 需要下载的长度
        /// </summary>
        public long RequirementDownloadLength
        {
            get => _requirementDownloadLength;
            set
            {
                _requirementDownloadLength = value;
                SegmentChanged?.Invoke(this, null);
            }
        }

        /// <summary>
        /// 已经下载的长度
        /// </summary>
        /// 下载的时候需要通告管理器
        public long DownloadedLength { get => _downloadedLength; set => _downloadedLength = value; }

        /// <summary>
        /// 分段管理
        /// </summary>
        public SegmentManager SegmentManager { set; get; }
    }

    // 下载管理在发现支持分段下载的时候给出事件
}
