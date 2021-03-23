using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.DotNETBuild.Context;
using dotnetCampus.DotNETBuild.Utils;
using dotnetCampus.GitCommand;
using dotnetCampus.Threading;

namespace CopyAfterCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var compileConfiguration = AppConfigurator.GetAppConfigurator().Of<CompileConfiguration>();

            Console.WriteLine(
                $"这个工具可以用来 CodeDirectory 的代码从 OriginBranch 的最新版每一个 commit 进行构建，将构建输出文件夹 OutputDirectory 的内容保存到 TargetDirectory 文件夹");

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

            var logFile = new FileInfo(Path.Combine(folder, "Log", $"{DateTime.Now:yyMMdd hhmmss}.txt"));
            Console.WriteLine($"日志文件{logFile}");

            var binaryChopCompiler = new BinaryChopCompiler(codeDirectory, targetDirectory, outputDirectory,
                originBranch, new FileLogger(logFile));
            binaryChopCompiler.CompileAllCommitAndCopy();

            Console.WriteLine("构建完成");
            Thread.Sleep(1000);
        }
    }

    interface ILogger
    {
        void Info(string str);
    }

    class FileLogger : ILogger
    {
        private FileLog FileLog { get; }

        public FileLogger(FileInfo logFile)
        {
            FileLog = new FileLog(logFile);
        }

        /// <inheritdoc />
        public void Info(string str)
        {
            Console.WriteLine(str);
            FileLog.WriteLine(str);
        }
    }
}