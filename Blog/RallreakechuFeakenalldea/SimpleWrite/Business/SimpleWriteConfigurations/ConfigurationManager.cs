using SimpleWrite.Foundation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

namespace SimpleWrite.Business.SimpleWriteConfigurations;

public class ConfigurationManager
{
    public ConfigurationManager(AppPathManager appPathManager)
    {
        var applicationConfigurationFile = appPathManager.ApplicationConfigurationFile;
        _isUserApplicationConfigurationFileExists = applicationConfigurationFile.IsExists();
        var fileConfigurationRepo = ConfigurationFactory.FromFile(applicationConfigurationFile, RepoSyncingBehavior.Sync);
        FileConfigurationRepo = fileConfigurationRepo;
        AppConfigurator = fileConfigurationRepo.CreateAppConfigurator();

        if (!_isUserApplicationConfigurationFileExists)
        {
            // 那如果当前所在存在配置文件呢？那就补充一下吧
            // 以下不是正常的分支，预期正常都不会进入
            var localConfigurationFile =
                Path.Join(AppContext.BaseDirectory, AppPathManager.ApplicationConfigurationFileName);
            if (File.Exists(localConfigurationFile))
            {
                var defaultConfiguration = AppConfigurator.Default;
                Task.Run(async () =>
                {
                    var text = await File.ReadAllTextAsync(localConfigurationFile);
                    foreach (var keyValuePair in CoinConfigurationSerializer.Deserialize(text))
                    {
                        defaultConfiguration[keyValuePair.Key] = keyValuePair.Value; 
                    }
                });
            }
        }
    }

    public FileConfigurationRepo FileConfigurationRepo { get; }

    public IAppConfigurator AppConfigurator { get; }

    /// <summary>
    /// 用户的配置文件是否存在
    /// </summary>
    private bool _isUserApplicationConfigurationFileExists;

    public void ReadConfiguration()
    {
        // 不需要明确写读取，将会在后台自动读取
    }
}