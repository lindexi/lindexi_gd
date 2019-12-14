using System;
using System.IO;
using System.Net;
using System.Reflection;
using dotnetCampus.Configurations;

namespace CopyAfterCompile
{
    /// <summary>
    /// 寻找文件，将找到的文件放在 AppConfigurator 配置
    /// </summary>
    public class FileSniff
    {
        public FileSniff()
        {
            CurrentDirectory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public void Sniff()
        {
            FindNugetFile();

            Console.WriteLine("寻找 nuget 文件完成");

            FindMsBuildFile();

            FindSlnFile();


        }

        private void FindSlnFile()
        {

        }

        private void FindMsBuildFile()
        {
            var vs2019Community =
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe";

            if (File.Exists(vs2019Community))
            {
                CompileConfiguration.Vs2019CommunityMsbuild = vs2019Community;
                CompileConfiguration.MsbuildFile = vs2019Community;
            }
        }

        private void FindNugetFile()
        {
            // 尝试寻找 NuGet 文件
            var path = Environment.GetEnvironmentVariable("Path");

            var file = Path.Combine(CurrentDirectory.FullName, "nuget.exe");

            if (File.Exists(file))
            {
                CompileConfiguration.NugetFile = file;
                return;
            }

            Console.WriteLine($"找不到 {file} 文件，正在下载");
            var webClient = new WebClient();
            webClient.DownloadFile(new Uri("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"), file);

            CompileConfiguration.NugetFile = file;
        }


        private DirectoryInfo CurrentDirectory { get; }

        public IAppConfigurator AppConfigurator => CopyAfterCompile.AppConfigurator.GetAppConfigurator();

        public CompileConfiguration CompileConfiguration => AppConfigurator.Of<CompileConfiguration>();
    }
}