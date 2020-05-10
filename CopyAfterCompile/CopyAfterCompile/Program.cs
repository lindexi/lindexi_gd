using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.GitCommand;
using dotnetCampus.Threading;

namespace CopyAfterCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            var compileConfiguration = AppConfigurator.GetAppConfigurator().Of<CompileConfiguration>();

            Console.WriteLine($"这个工具可以用来 CodeDirectory 的代码从 OriginBranch 的最新版每一个 commit 进行构建，将构建输出文件夹 OutputDirectory 的内容保存到 TargetDirectory 文件夹");

            Console.WriteLine($"代码仓库文件夹 {compileConfiguration.CodeDirectory}");
            Console.WriteLine($"代码构建输出文件夹 {compileConfiguration.OutputDirectory}");
            Console.WriteLine($"保存的文件夹 {compileConfiguration.TargetDirectory}");

            Console.WriteLine($"构建代码分支 {compileConfiguration.OriginBranch}");

            var codeDirectory = new DirectoryInfo(compileConfiguration.CodeDirectory);
            var outputDirectory = new DirectoryInfo(compileConfiguration.OutputDirectory);
            var targetDirectory = new DirectoryInfo(compileConfiguration.TargetDirectory);
            var originBranch = compileConfiguration.OriginBranch;

            if (!codeDirectory.Exists)
            {
                throw new ArgumentException("代码仓库文件夹 CodeDirectory 找不到");
            }

            Directory.CreateDirectory(targetDirectory.FullName);

            var binaryChopCompiler = new BinaryChopCompiler(codeDirectory, targetDirectory, outputDirectory,
                originBranch, compileConfiguration.LastCommit);
            binaryChopCompiler.CompileAllCommitAndCopy();
        }
    }

    interface ILogger
    {
        void Info(string str);
    }

    class FileLogger : ILogger
    {
        private LogFileManager LogFileManager { get; } = new LogFileManager();

        /// <inheritdoc />
        public void Info(string str)
        {
            Console.WriteLine(str);
            LogFileManager.WriteLine(str);
        }
    }

    public class LogFileManager
    {
        public LogFileManager()
        {
            WriteToFile();
        }

        public static DirectoryInfo LogFolder { set; get; } = new DirectoryInfo("Log");

        public FileInfo LogFile { set; get; }

        public static void CleanLogFile()
        {
            if (LogFolder == null) return;
            try
            {
                var time = TimeSpan.FromDays(7);
                foreach (var temp in LogFolder.GetFiles())
                {
                    if (DateTime.Now - temp.CreationTime > time)
                    {
                        try
                        {
                            temp.Delete();
                        }
                        catch (Exception)
                        {
                            // 删除文件
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void WriteLine(string message)
        {
            var time = DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss.ffffff");
            _cache.Enqueue($"{time} {message}");

            _asyncAutoResetEvent.Set();
        }

        private readonly AsyncAutoResetEvent _asyncAutoResetEvent = new AsyncAutoResetEvent(false);

        private readonly ConcurrentQueue<string> _cache = new ConcurrentQueue<string>();

        private void WriteToFile()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await _asyncAutoResetEvent.WaitOneAsync();

                    var count = Math.Max(_cache.Count, 8);

                    var cache = new List<string>(count);

                    while (_cache.TryDequeue(out var message))
                    {
                        cache.Add(message);
                    }

                    if (LogFile is null)
                    {
                        var folder = LogFolder?.FullName ?? "";
                        if (!string.IsNullOrEmpty(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        var id = Process.GetCurrentProcess().Id;
                        var time = DateTime.Now.ToString("yyMMddhhmmss");
                        var file = Path.Combine(folder, $"{time} {id}.txt");

                        LogFile = new FileInfo(file);
                    }

                    await File.AppendAllLinesAsync(LogFile.FullName, cache);
                }
            });
        }
    }
}
