using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;

using Renci.SshNet;

namespace SshCore;

internal class Program
{
    static async Task Main(string[] args)
    {
        var file = @"c:\lindexi\CA\ssh.coin";
        var fileConfigurationRepo = ConfigurationFactory.FromFile(file, RepoSyncingBehavior.Sync);
        var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();

        var sshConfiguration = appConfigurator.Of<SshConfiguration>();
        sshConfiguration.Host = "127.0.0.1";
        sshConfiguration.UserName = "root";
        sshConfiguration.Password = "lindexi";



        //var sshClient = new SshClient(new ConnectionInfo());

        await fileConfigurationRepo.SaveAsync();
    }
}

class SshConfiguration : Configuration
{
    public SshConfiguration() : base("")
    {
    }

    public string Host
    {
        get => GetString();
        set => SetValue(value);
    }

    public int Port
    {
        get => GetInt32() ?? 22;
        set => SetValue(value);
    }

    public string UserName
    {
        get => GetString();
        set => SetValue(value);
    }

    public string Password
    {
        get => GetString();
        set => SetValue(value);
    }
}