using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using dotnetCampus.DotNETBuild.Context;
using dotnetCampus.DotNETBuild.Utils;
using dotnetCampus.GitCommand;
using Walterlv.IO.PackageManagement;

namespace CopyAfterCompile
{
    /// <summary>
    /// 对二分查找做准备，编译每个提交
    /// </summary>
    class BinaryChopCompiler
    {
        /// <inheritdoc />
        public BinaryChopCompiler(DirectoryInfo codeDirectory,
            DirectoryInfo targetDirectory,
            DirectoryInfo outputDirectory = null,
            string originBranch = null,
            //ICompiler compiler = null,
            ILogger logger = null)
        {
            CodeDirectory = codeDirectory;
            TargetDirectory = targetDirectory;

            Logger = logger;

            if (!string.IsNullOrEmpty(originBranch))
            {
                OriginBranch = originBranch;
            }

            var git = new Git(codeDirectory);

            _git = git;
            //Compiler = compiler ?? new MsBuildCompiler();

            if (outputDirectory is null)
            {
                outputDirectory = new DirectoryInfo(Path.Combine(codeDirectory.FullName, "bin"));
            }

            OutputDirectory = outputDirectory;
        }

        private ILogger Logger { get; }

        private void Log(string str) => Logger?.Info(str);

        private readonly Git _git;

        public string OriginBranch { get; } = "dev";

        //private ICompiler Compiler { get; }

        /// <summary>
        /// 移动到的文件夹，编译完成将输出移动到这个文件夹
        /// </summary>
        public DirectoryInfo TargetDirectory { get; }

        public DirectoryInfo CodeDirectory { get; }

        /// <summary>
        /// 输出文件夹
        /// </summary>
        public DirectoryInfo OutputDirectory { get; }

        public void CompileAllCommitAndCopy()
        {
            _git.FetchAll();

            var commitList = GetCommitList().Reverse().ToList();

            foreach (var commit in commitList)
            {
                try
                {
                    Log($"开始 {commit} 二分");
                    CleanDirectory(commit);

                    var appConfigurator = GetCurrentBuildConfiguration();

                    var currentBuildLogFile = GetCurrentBuildLogFile(appConfigurator);

                    var msBuildCompiler = new MsBuildCompiler(appConfigurator);
                    msBuildCompiler.Compile();

                    MoveFile(commit, currentBuildLogFile);

                    LastCommit = commit;
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }
            }
        }

        private static FileInfo GetCurrentBuildLogFile(IAppConfigurator appConfigurator)
        {
            var currentBuildLogFile = new FileInfo(Path.GetTempFileName());
            var logConfiguration = appConfigurator.Of<LogConfiguration>();
            logConfiguration.BuildLogFile = currentBuildLogFile.FullName;
            return currentBuildLogFile;
        }

        /// <summary>
        /// 获取当前构建的配置
        /// </summary>
        /// <returns></returns>
        private IAppConfigurator GetCurrentBuildConfiguration()
        {
            // 这是在每次构建的时候，放在代码仓库的构建代码
            var currentBuildConfiguration = Path.Combine(CompileConfiguration.CodeDirectory, "Build.coin");
            var fileConfigurationRepo = ConfigurationFactory.FromFile(currentBuildConfiguration);
            var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();
            var compileConfiguration = appConfigurator.Of<CompileConfiguration>();
            compileConfiguration.CodeDirectory = CompileConfiguration.CodeDirectory;

            var toolConfiguration = appConfigurator.Of<ToolConfiguration>();
            var nugetFile = new FileInfo("nuget.exe");
            if (nugetFile.Exists)
            {
                toolConfiguration.NugetPath = nugetFile.FullName;
            }

            return appConfigurator;
        }

        private void MoveFile(string commit, FileInfo buildLogFile)
        {
            var outputDirectory = new DirectoryInfo(OutputDirectory.FullName);

            var moveDirectory = Path.Combine(TargetDirectory.FullName, commit);
            Log($"将{outputDirectory.FullName}移动到{moveDirectory}");

            PackageDirectory.Move(outputDirectory, new DirectoryInfo(moveDirectory));

            if (File.Exists(buildLogFile.FullName))
            {
                try
                {
                    Directory.CreateDirectory(moveDirectory);
                    var logFile = Path.Combine(moveDirectory, "BuildLog.txt");
                    buildLogFile.CopyTo(logFile);
                    File.Delete(buildLogFile.FullName);
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private void CleanDirectory(string commit)
        {
            Log($"开始清空仓库");

            var git = _git;
            git.Clean();
            git.Checkout(commit);
        }

        private string[] GetCommitList()
        {
            var git = _git;
            if (LastCommit is null)
            {
                return git.GetLogCommit();
            }
            else
            {
                return git.GetLogCommit(LastCommit, OriginBranch);
            }
        }

        private string LastCommit
        {
            set => CompileConfiguration.LastCommit = value;
            get => CompileConfiguration.LastCommit;
        }

        private IAppConfigurator AppConfigurator =>
            dotnetCampus.DotNETBuild.Context.AppConfigurator.GetAppConfigurator();

        private CompileConfiguration CompileConfiguration => AppConfigurator.Of<CompileConfiguration>();
    }
}