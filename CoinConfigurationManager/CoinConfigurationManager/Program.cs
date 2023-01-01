using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace CoinConfigurationManager;

//internal class Program
//{
//    static void Main(string[] args)
//    {
//        Console.WriteLine("Hello, World!");
//    }
//}

[TestClass]
public class CoinConfigurationManagerTest
{
    [ContractTestCase]
    public void Install()
    {

    }
}

class ManifestConfiguration : Configuration
{
    public string Id
    {
        set => SetValue(value);
        get => GetString();
    }

    public string DisplayName
    {
        set => SetValue(value);
        get => GetString();
    }

    public string Description
    {
        set => SetValue(value);
        get => GetString();
    }

    public string SupportMinClientVersion
    {
        set => SetValue(value);
        get => GetString();
    }
}

class CoinConfigurationAppManager
{
    public CoinConfigurationAppManager(IAppConfigurator mainConfigurator, DirectoryInfo storageDirectory)
    {
        _mainConfigurator = mainConfigurator;
        _storageDirectory = storageDirectory;
    }

    private readonly IAppConfigurator _mainConfigurator;
    private readonly DirectoryInfo _storageDirectory;

    public async Task Install(FileInfo manifestFile, FileInfo configurationFile)
    {
        var manifestConfiguration = await ToMemoryConfigurationRepoAsync(manifestFile);
        var manifestAppConfigurator = manifestConfiguration.CreateAppConfigurator();
        var configuration = manifestAppConfigurator.Of<ManifestConfiguration>();


    }

    private async Task<MemoryConfigurationRepo> ToMemoryConfigurationRepoAsync(FileInfo file)
    {
        using var fileStream = file.OpenRead();
        using var streamReader = new StreamReader(fileStream);

        var text = await streamReader.ReadToEndAsync();
        var dictionary = CoinConfigurationSerializer.Deserialize(text);
        var memoryConfigurationRepo = new MemoryConfigurationRepo(dictionary);
        return memoryConfigurationRepo;
    }
}