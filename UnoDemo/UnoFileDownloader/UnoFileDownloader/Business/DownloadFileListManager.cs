using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using UnoFileDownloader.Utils;

namespace UnoFileDownloader.Business
{
    public class DownloadFileListManager
    {
        public ObservableCollection<DownloadFileInfo> DownloadFileInfoList { get; } = new ObservableCollection<DownloadFileInfo>();

        public async Task InitAsync()
        {
            var list = await ReadDownloadedFileListAsync();
            if (list != null)
            {
                DownloadFileInfoList.AddRange(list);
            }
        }

        public Task SaveAsync() => WriteDownloadedFileListToFileAsync(DownloadFileInfoList.ToList());

        public async Task AddDownloadFileAsync(DownloadFileInfo downloadFileInfo)
        {
            DownloadFileInfoList.Add(downloadFileInfo);
            await SaveAsync();
        }

        /// <summary>
        /// 读取本地存储的下载列表
        /// </summary>
        /// <returns></returns>
        private async Task<List<DownloadFileInfo>?> ReadDownloadedFileListAsync()
        {
            var file = GetStorageFilePath();

            if (!File.Exists(file))
            {
                return null;
            }

            var text = await File.ReadAllTextAsync(file);

            var downloadFileInfoList = JsonSerializer.Deserialize<List<DownloadFileInfo>>(text);
            return downloadFileInfoList;
        }

        /// <summary>
        /// 写入下载列表
        /// </summary>
        /// <param name="downloadFileInfoList"></param>
        /// <returns></returns>
        private async Task WriteDownloadedFileListToFileAsync(List<DownloadFileInfo> downloadFileInfoList)
        {
            var file = GetStorageFilePath();

            var text = JsonSerializer.Serialize(downloadFileInfoList);

            await File.WriteAllTextAsync(file, text);
        }

        private string GetStorageFilePath()
        {
            string folder = AppContext.BaseDirectory;
            var file = Path.Join(folder, StorageFile);

            return file;
        }

        private const string StorageFile = "DownloadedFileList.json";
    }
}
