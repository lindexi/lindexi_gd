using SimpleWrite.Foundation;

using System;
using System.Collections.Generic;
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
        var fileConfigurationRepo = ConfigurationFactory.FromFile(applicationConfigurationFile,RepoSyncingBehavior.Sync);
        FileConfigurationRepo = fileConfigurationRepo;
        AppConfigurator = fileConfigurationRepo.CreateAppConfigurator();
    }

    public FileConfigurationRepo FileConfigurationRepo { get; set; }

    public IAppConfigurator AppConfigurator { get; set; }

    public void ReadConfiguration()
    {
        // 不需要明确写读取，将会在后台自动读取
    }
}
