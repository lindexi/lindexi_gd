using System;

namespace FileDownloader
{
    /// <summary>
    /// 下载的段，这个段的内容和长度将会不断更改
    /// </summary>
    public class DownloadSegment
    {
        private long _startPoint;
        private long _downloadedLength;
        private long _requirementDownloadPoint;

        public event EventHandler SegmentChanged;

        public DownloadSegment(long startPoint)
        {
            _startPoint = startPoint;
        }

        public DownloadSegment(long startPoint, long requirementDownloadPoint)
        {
            _startPoint = startPoint;
            _requirementDownloadPoint = requirementDownloadPoint;
        }

        public long StartPoint
        {
            get => _startPoint;
        }

        /// <summary>
        /// 需要下载到的点
        /// </summary>
        public long RequirementDownloadPoint
        {
            set
            {
                _requirementDownloadPoint = value;
                SegmentChanged?.Invoke(this, null);
            }
            get => _requirementDownloadPoint;
        }

        public override string ToString()
        {
            return $"Start={StartPoint} Require={RequirementDownloadPoint} Download={DownloadedLength}/{RequirementDownloadPoint - StartPoint}";
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
        /// 当前的下载点
        /// </summary>
        /// 需要处理多线程访问
        public long CurrentDownloadPoint => StartPoint + DownloadedLength;

        /// <summary>
        /// 分段管理
        /// </summary>
        public SegmentManager SegmentManager { set; get; }
    }
}