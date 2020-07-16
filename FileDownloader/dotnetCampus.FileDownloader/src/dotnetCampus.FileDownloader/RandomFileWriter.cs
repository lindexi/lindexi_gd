using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Threading;

namespace dotnetCampus.FileDownloader
{
    /// <summary>
    /// 不按照顺序，随机写入文件
    /// </summary>
    public class RandomFileWriter : IAsyncDisposable
    {
        public RandomFileWriter(FileStream stream)
        {
            Stream = stream;
            Task.Run(WriteToFile);
        }

        public void WriteAsync(long fileStartPoint, byte[] data, int dataLength)
        {
            var fileSegment = new FileSegment(fileStartPoint, data, dataLength);
            DownloadSegmentList.Enqueue(fileSegment);
        }

        private async Task WriteToFile()
        {
            while (true)
            {
                var fileSegment = await DownloadSegmentList.DequeueAsync();

                Stream.Seek(fileSegment.FileStartPoint, SeekOrigin.Begin);

                await Stream.WriteAsync(fileSegment.Data, 0, fileSegment.DataLength);

                // 释放缓存
                SharedArrayPool.Return(fileSegment.Data);

                var isEmpty = DownloadSegmentList.Count == 0;

                if (isEmpty)
                {
                    WriteFinished?.Invoke(this, EventArgs.Empty);
                    if (_isDispose)
                    {
                        return;
                    }
                }
            }
        }

        private event EventHandler WriteFinished;

        private FileStream Stream { get; }

        private AsyncQueue<FileSegment> DownloadSegmentList { get; } = new AsyncQueue<FileSegment>();

        readonly struct FileSegment
        {
            public FileSegment(long fileStartPoint, byte[] data, int dataLength)
            {
                FileStartPoint = fileStartPoint;
                Data = data;
                DataLength = dataLength;
            }

            /// <summary>
            /// 文件开始写入
            /// </summary>
            public long FileStartPoint { get; }

            public byte[] Data { get; }

            public int DataLength { get; }
        }

        public async ValueTask DisposeAsync()
        {
            _isDispose = true;

            if (DownloadSegmentList.Count == 0)
            {
                return;
            }

            var task = new TaskCompletionSource<bool>();

            WriteFinished += (sender, args) =>
            {
                task.SetResult(true);
                WriteFinished = null;
            };

            if (DownloadSegmentList.Count == 0)
            {
                return;
            }

            await task.Task;
        }

        private bool _isDispose;
    }

    public static class SharedArrayPool
    {
        public static byte[] Rent(int minLength)
        {
            return ArrayPool<byte>.Shared.Rent(minLength);
        }

        public static void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}