using System;

namespace FileDownloader
{
    /// <summary>
    /// 下载的段，这个段的内容和长度将会不断更改
    /// </summary>
    public class DownloadSegment
    {
        private long _startPoint;
        private long _requirementDownloadLength;
        private long _downloadedLength;

        public event EventHandler SegmentChanged;

        public DownloadSegment(long startPoint)
        {
            _startPoint = startPoint;
        }

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
        public long DownloadedLength
        {
            get => _downloadedLength;
            set
            {
                // 不支持越下载内容越小
                _downloadedLength = value;
                SegmentManager.RegisterCurrentDownload(StartPoint, value);
            }
        }

        /// <summary>
        /// 分段管理
        /// </summary>
        public SegmentManager SegmentManager { set; get; }
    }
}