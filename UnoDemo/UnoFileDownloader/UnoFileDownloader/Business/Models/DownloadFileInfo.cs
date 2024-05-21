using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UnoFileDownloader.Business.Models
{
    public class DownloadFileInfo : INotifyPropertyChanged
    {
        private string _downloadProcess = string.Empty;
        private string _fileSize = string.Empty;
        private string _downloadSpeed = string.Empty;
        private bool _isFinished = false;
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        ///     文件的大小
        /// </summary>
        /// 也许服务器骗我，因此文件的大小也许会更改
        public string FileSize
        {
            get => _fileSize;
            set
            {
                if (value == _fileSize) return;
                _fileSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     下载进度，用于绑定
        /// </summary>
        public string DownloadProcess
        {
            get => _downloadProcess;
            set
            {
                if (value == _downloadProcess) return;
                _downloadProcess = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     下载项被加入的时间
        /// </summary>
        /// 加入时间不可变，因此不带通知
        public string AddedTime { get; init; } = string.Empty;

        /// <summary>
        ///     下载链接
        /// </summary>
        /// 下载链接不可变，因此不带通知
        public string DownloadUrl { get; init; } = string.Empty;

        /// <summary>
        ///     下载的本地文件保存路径
        /// </summary>
        /// 下载的本地文件保存路径不可变，因此不带通知
        public string FilePath { get; init; } = string.Empty;

        /// <summary>
        ///     当前下载的速度
        /// </summary>
        [JsonIgnore]
        public string DownloadSpeed
        {
            get => _downloadSpeed;
            set
            {
                if (value == _downloadSpeed) return;
                _downloadSpeed = value;
                OnPropertyChanged();
            }
        }

        public bool IsFinished
        {
            get => _isFinished;
            set
            {
                if (value == _isFinished) return;
                _isFinished = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            //var dispatcher = Application.Current.Dispatcher;
            //if (dispatcher.CheckAccess())
            //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //else
            //    dispatcher.InvokeAsync(() =>
            //    {
            //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //    });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
