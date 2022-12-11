using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

namespace Workbench;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var hotKey = new HotKey();
        hotKey.Start();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var task = Task.Run(() =>
        {
            // 用来切走同步上下文
            ApplicationConfiguration.FileConfigurationRepo.SaveAsync();
        });
        task.Wait();

        base.OnExit(e);
    }
}

public static class ApplicationConfiguration
{
    static ApplicationConfiguration()
    {
        var fileConfigurationRepo = ConfigurationFactory.FromFile(PathManager.Configuration.FullName);
        AppConfigurator = fileConfigurationRepo.CreateAppConfigurator();

        FileConfigurationRepo = fileConfigurationRepo;
    }

    public static FileConfigurationRepo FileConfigurationRepo { get; }
    public static IAppConfigurator AppConfigurator { get; }
}

public static class PathManager
{
    static PathManager()
    {
        WorkingFolder = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "lindexi", "Workbench"));


        Configuration = new FileInfo(Path.Combine(WorkingFolder.FullName, "Configuration.coin"));
    }

    public static DirectoryInfo WorkingFolder { get; }

    public static FileInfo Configuration { get; }
}