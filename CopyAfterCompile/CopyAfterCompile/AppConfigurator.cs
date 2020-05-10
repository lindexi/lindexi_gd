using System;
using System.IO;
using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

namespace CopyAfterCompile
{
    /// <summary>
    ///应用配置
    /// </summary>
    public static class AppConfigurator
    {
        /// <summary>
        /// 设置配置文件路径
        /// </summary>
        /// <param name="file"></param>
        public static void SetConfigurationFile(FileInfo file)
        {
            if (_appConfigurator != null)
            {
                throw new Exception("必须在调用 GetAppConfigurator 方法之前设置配置文件路径，请将设置路径的代码放在程序运行最前");
            }

            ConfigurationFile = file;
        }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public static FileInfo ConfigurationFile { get; private set; } = new FileInfo("Build.coin");

        public static IAppConfigurator GetAppConfigurator()
        {
            if (_appConfigurator is null)
            {
                var fileConfigurationRepo = new FileConfigurationRepo(ConfigurationFile.FullName);
                _appConfigurator = fileConfigurationRepo.CreateAppConfigurator();
            }

            return _appConfigurator;
        }

        private static IAppConfigurator _appConfigurator;
    }
}