using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Threading;

namespace dotnetCampus.FastFileLogger
{
    public class FileLogger
    {
        public FileLogger()
        {
            _bufferTaskDoUtilInitialized = new DoubleBufferTaskDoUtilInitialized<string>(WriteLinesToFile);
        }

        public void Initialize(FileInfo logFile)
        {
            var directory = logFile.Directory;
            directory!.Create();

            _logFilePath = logFile.FullName;

            _bufferTaskDoUtilInitialized.OnInitialized();
        }

        public void QueueWriteLineToFile(string text)
        {
            _bufferTaskDoUtilInitialized.AddTask(text);
        }

        public Task DisposeAsync()
        {
            _bufferTaskDoUtilInitialized.Finish();
            return _bufferTaskDoUtilInitialized.WaitAllTaskFinish();
        }

        private Task WriteLinesToFile(List<string> textList)
        {
            return File.AppendAllLinesAsync(_logFilePath, textList);
        }

        private string _logFilePath;

        private readonly DoubleBufferTaskDoUtilInitialized<string> _bufferTaskDoUtilInitialized;
    }
}