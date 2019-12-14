using System.IO;

namespace CopyAfterCompile
{
    /// <summary>
    /// 编译配置
    /// </summary>
    class CompileConfiguration
    {
        public FileInfo SlnPath { get; set; }

        public FileInfo NugetFile { get; set; }

        public FileInfo MsbuildFile { get; set; }

        public string CurrentCommit { get; set; }

        /// <summary>
        /// 输出文件夹
        /// </summary>
        public DirectoryInfo OutputDirectory { get; set; }

        /// <summary>
        /// 打包输出文件夹
        /// </summary>
        public DirectoryInfo NupkgDirectory { get; set; }

        public string BuildVersion { get; set; }

        /// <summary>
        /// 代码文件夹
        /// </summary>
        public DirectoryInfo CodeDirectory { get; set; }
    }
}