using System.IO;
using dotnetCampus.Configurations;

namespace CopyAfterCompile
{
    /// <summary>
    /// 编译配置
    /// </summary>
    public class CompileConfiguration : Configuration
    {
        public string OriginBranch
        {
            set => SetValue(value);
            get => GetString() ?? "origin/dev";
        }

        /// <summary>
        /// 保存构建完成的文件夹
        /// </summary>
        public string TargetDirectory
        {
            set => SetValue(value);
            get => GetString();
        }

        public string LastCommit
        {
            set => SetValue(value);
            get => GetString();
        }

        public string SlnPath
        {
            set
            {
                SetValue(value);
            }
            get { return GetString(); }
        }

        public string NugetFile
        {
            set
            {
                SetValue(value);
            }
            get { return GetString(); }
        }

        public string MsbuildFile
        {
            set
            {
                SetValue(value);
            }
            get { return GetString(); }
        }

        public string Vs2019CommunityMsbuild
        {
            set
            {
                SetValue(value);
            }
            get { return GetString(); }
        }

        public string CurrentCommit
        {
            set => SetValue(value);
            get => GetString();
        }

        /// <summary>
        /// 输出文件夹
        /// </summary>
        public string OutputDirectory
        {
            set => SetValue(value);
            get => GetString();
        }

        /// <summary>
        /// 打包输出文件夹
        /// </summary>
        public string NupkgDirectory
        {
            set => SetValue(value);
            get => GetString();
        }

        public string BuildVersion
        {
            set => SetValue(value);
            get => GetString();
        }

        /// <summary>
        /// 代码文件夹
        /// </summary>
        public string CodeDirectory
        {
            set => SetValue(value);
            get => GetString();
        }
    }
}