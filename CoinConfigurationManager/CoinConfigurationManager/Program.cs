using System.Runtime.CompilerServices;

using Castle.Core.Configuration;

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
    public void Uninstall()
    {
        "卸载插件时，插件影响的配置项被更改，将不会清配置".Test(async () =>
        {
            var memoryConfigurationRepo = new MemoryConfigurationRepo();
            var mainConfigurator = memoryConfigurationRepo.CreateAppConfigurator();

            var storageDirectory = Directory.CreateDirectory(Path.GetRandomFileName());
            var coinConfigurationAppManager = new CoinConfigurationAppManager(mainConfigurator, storageDirectory);

            // 先安装三个插件，再卸载其中的一个插件
            var count = 3;
            var idList = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var manifestFilePath = Path.GetTempFileName();
                var configurationFilePath = Path.GetTempFileName();

                var manifestFileConfigurationRepo = ConfigurationFactory.FromFile(manifestFilePath);
                var manifestConfiguration = manifestFileConfigurationRepo.CreateAppConfigurator().Of<ManifestConfiguration>();
                FillTestManifestConfiguration(manifestConfiguration);
                await manifestFileConfigurationRepo.SaveAsync();

                // 每个配置不相同，可以测试卸载是否可以删除配置
                var testConfigurationDictionary = new Dictionary<string, string?>()
                {
                    {$"Test{i}","Test1"}
                };
                var configurationText = CoinConfigurationSerializer.Serialize(testConfigurationDictionary);
                await File.WriteAllTextAsync(configurationFilePath, configurationText);

                await coinConfigurationAppManager.InstallAsync(new FileInfo(manifestFilePath), new FileInfo(configurationFilePath));

                idList.Add(manifestConfiguration.Id);
            }

            // 更改插件的配置项，测试卸载
            var value = "123123123123123";
            mainConfigurator.Default["Test1"] = value;

            // 卸载一个插件
            await coinConfigurationAppManager.UninstallAsync(idList[1]);

            Assert.AreEqual(value, mainConfigurator.Default["Test1"].ToString());
        });

        "插件卸载之后，配置将被清掉，列举安装的插件时，也不会列举到被卸载的插件".Test(async () =>
        {
            var memoryConfigurationRepo = new MemoryConfigurationRepo();
            var mainConfigurator = memoryConfigurationRepo.CreateAppConfigurator();

            var storageDirectory = Directory.CreateDirectory(Path.GetRandomFileName());
            var coinConfigurationAppManager = new CoinConfigurationAppManager(mainConfigurator, storageDirectory);

            // 先安装三个插件，再卸载其中的一个插件
            var count = 3;
            var idList = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var manifestFilePath = Path.GetTempFileName();
                var configurationFilePath = Path.GetTempFileName();

                var manifestFileConfigurationRepo = ConfigurationFactory.FromFile(manifestFilePath);
                var manifestConfiguration = manifestFileConfigurationRepo.CreateAppConfigurator().Of<ManifestConfiguration>();
                FillTestManifestConfiguration(manifestConfiguration);
                await manifestFileConfigurationRepo.SaveAsync();

                // 每个配置不相同，可以测试卸载是否可以删除配置
                var testConfigurationDictionary = new Dictionary<string, string?>()
                {
                    {$"Test{i}","Test1"}
                };
                var configurationText = CoinConfigurationSerializer.Serialize(testConfigurationDictionary);
                await File.WriteAllTextAsync(configurationFilePath, configurationText);

                await coinConfigurationAppManager.InstallAsync(new FileInfo(manifestFilePath), new FileInfo(configurationFilePath));

                idList.Add(manifestConfiguration.Id);
            }

            // 卸载一个插件
            await coinConfigurationAppManager.UninstallAsync(idList[1]);

            // 被卸载的插件的配置可以删除，其他插件不会被影响
            Assert.IsNull(mainConfigurator.Default["Test1"]);
            Assert.IsNotNull(mainConfigurator.Default["Test0"]);
            Assert.IsNotNull(mainConfigurator.Default["Test2"]);

            await foreach (var manifestConfiguration in coinConfigurationAppManager.GetInstalledConfigurationListAsync())
            {
                Assert.AreEqual(1, idList.RemoveAll(id => id == manifestConfiguration.Id));
            }
            // 被卸载一个，于是列举所有安装的插件，将少一个
            Assert.AreEqual(1, idList.Count);
        });
    }

    [ContractTestCase]
    public void GetInstalledConfigurationList()
    {
        "安装五个插件之后，可以获取到安装的五个插件".Test(async () =>
        {
            var memoryConfigurationRepo = new MemoryConfigurationRepo();
            var mainConfigurator = memoryConfigurationRepo.CreateAppConfigurator();

            var storageDirectory = Directory.CreateDirectory(Path.GetRandomFileName());
            var coinConfigurationAppManager = new CoinConfigurationAppManager(mainConfigurator, storageDirectory);

            var count = 5;
            var idList = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var manifestFilePath = Path.GetTempFileName();
                var configurationFilePath = Path.GetTempFileName();

                var manifestFileConfigurationRepo = ConfigurationFactory.FromFile(manifestFilePath);
                var manifestConfiguration = manifestFileConfigurationRepo.CreateAppConfigurator().Of<ManifestConfiguration>();
                FillTestManifestConfiguration(manifestConfiguration);
                await manifestFileConfigurationRepo.SaveAsync();

                await coinConfigurationAppManager.InstallAsync(new FileInfo(manifestFilePath), new FileInfo(configurationFilePath));

                idList.Add(manifestConfiguration.Id);
            }

            // 判断是否五个插件安装的方法就是遍历每一个，拿到一个删除一个，要求能够删除到一个。因为能删除到是一个那就证明 id 存在列表里面。最后判断列表数量是 0 个就可以测试是否安装五个插件，也就是列表里面所有 id 都被删除完成
            await foreach (var manifestConfiguration in coinConfigurationAppManager.GetInstalledConfigurationListAsync())
            {
                Assert.AreEqual(1, idList.RemoveAll(id => id == manifestConfiguration.Id));
            }
            Assert.AreEqual(0, idList.Count);
        });
    }

    [ContractTestCase]
    public void Install()
    {
        "调用安装配置插件，可以将清单和配置拷贝到配置文件存储文件夹，且将配置信息写入到配置里".Test(async () =>
        {
            var memoryConfigurationRepo = new MemoryConfigurationRepo();
            var mainConfigurator = memoryConfigurationRepo.CreateAppConfigurator();

            var storageDirectory = Directory.CreateDirectory(Path.GetRandomFileName());

            var manifestFilePath = Path.GetTempFileName();
            var configurationFilePath = Path.GetTempFileName();

            var manifestFileConfigurationRepo = ConfigurationFactory.FromFile(manifestFilePath);
            var manifestConfiguration = manifestFileConfigurationRepo.CreateAppConfigurator().Of<ManifestConfiguration>();
            FillTestManifestConfiguration(manifestConfiguration);
            await manifestFileConfigurationRepo.SaveAsync();

            var testConfigurationDictionary = new Dictionary<string, string>()
            {
                {"Test","Test1"},
                {"Test1.Foo","Test1"},
                {"Test2.Foo123","Test1 123"},
                {"Test3.Foo121233","Test1 123"},
                {"Test3.Foo123.123","Test1 123"},
            };

            var fileConfigurationRepo = ConfigurationFactory.FromFile(configurationFilePath);
            var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();
            foreach (var (key, value) in testConfigurationDictionary)
            {
                appConfigurator.Default[key] = value;
            }

            await fileConfigurationRepo.SaveAsync();

            var coinConfigurationAppManager = new CoinConfigurationAppManager(mainConfigurator, storageDirectory);
            await coinConfigurationAppManager.InstallAsync(new FileInfo(manifestFilePath), new FileInfo(configurationFilePath));

            var installFolder = Path.Join(storageDirectory.FullName, manifestConfiguration.Id);
            Assert.AreEqual(true, Directory.Exists(installFolder));

            var memoryStorageDictionary = memoryConfigurationRepo.GetMemoryStorageDictionary();
            foreach (var (key, value) in testConfigurationDictionary)
            {
                Assert.AreEqual(true, memoryStorageDictionary.TryGetValue(key, out var storageValue));
                Assert.AreEqual(value, storageValue);
            }

            Assert.AreEqual(true, File.Exists(Path.Join(installFolder, CoinConfigurationAppManager.ManifestFileName)));

            AssertFileEquals(new FileInfo(configurationFilePath),
                new FileInfo(Path.Join(installFolder, CoinConfigurationAppManager.ConfigurationFileName)));
        });
    }

    private static void AssertFileEquals(FileInfo expected, FileInfo actual)
    {
        expected.Refresh();
        actual.Refresh();

        Assert.AreEqual(true, expected.Exists);
        Assert.AreEqual(true, actual.Exists);

        Assert.AreEqual(expected.Length, actual.Length);

        var expectedText = File.ReadAllText(expected.FullName);
        var actualText = File.ReadAllText(actual.FullName);

        Assert.AreEqual(expectedText, actualText);
    }

    private void FillTestManifestConfiguration(ManifestConfiguration manifestConfiguration)
    {
        manifestConfiguration.Id = Guid.NewGuid().ToString();
        manifestConfiguration.DisplayName = "测试配置";
        manifestConfiguration.Description = "测试";
        manifestConfiguration.SupportMinClientVersion = "1.0.0";
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

class ReadOnlyConfigurationRepo : ConfigurationRepo
{
    public ReadOnlyConfigurationRepo(Dictionary<string, string> configuration)
    {
        _configuration = configuration;
    }

    private readonly Dictionary<string, string> _configuration;

    public override string? GetValue(string key)
    {
        if (_configuration.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

    public override void SetValue(string key, string? value)
    {
        throw new NotSupportedException();
    }

    public override void ClearValues(Predicate<string> keyFilter)
    {
        throw new NotSupportedException();
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

    public async Task InstallAsync(FileInfo manifestFile, FileInfo configurationFile)
    {
        var manifestConfiguration = await ToMemoryConfigurationRepoAsync(manifestFile);
        var manifestAppConfigurator = manifestConfiguration.CreateAppConfigurator();
        var configuration = manifestAppConfigurator.Of<ManifestConfiguration>();

        var installFolder = Path.Join(_storageDirectory.FullName, configuration.Id);
        // 判断重复安装
        if (CheckInstalled(installFolder))
        {
            // 如果重复安装了，那就试试删掉之前的
            Directory.Delete(installFolder);
        }

        _ = Directory.CreateDirectory(installFolder);

        // 先拷贝 Manifest 文件和配置文件
        var targetManifestFilePath = Path.Join(installFolder, ManifestFileName);
        _ = manifestFile.CopyTo(targetManifestFilePath);

        var targetConfigurationFilePath = Path.Join(installFolder, ConfigurationFileName);
        _ = configurationFile.CopyTo(targetConfigurationFilePath);

        // 更新应用配置
        var configurationDictionary = await ToConfigurationDictionaryAsync(configurationFile);
        foreach (var (key, value) in configurationDictionary)
        {
            _mainConfigurator.Default[key] = value;

            // 安装这里可选记录原有的值，卸载时给还原。但是现在没有此需求，就不加上这个逻辑
        }
    }

    public async IAsyncEnumerable<ManifestConfiguration> GetInstalledConfigurationListAsync()
    {
        foreach (var directory in _storageDirectory.EnumerateDirectories())
        {
            var manifestFile = Path.Join(directory.FullName, ManifestFileName);
            if (File.Exists(manifestFile))
            {
                var text = await File.ReadAllTextAsync(manifestFile);
                var configuration = CoinConfigurationSerializer.Deserialize(text);
                var readOnlyConfigurationRepo = new ReadOnlyConfigurationRepo(configuration);
                var manifestConfiguration = readOnlyConfigurationRepo.CreateAppConfigurator().Of<ManifestConfiguration>();
                yield return manifestConfiguration;
            }
        }
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    /// <param name="id"></param>
    public async Task UninstallAsync(string id)
    {
        // 插件所安装的文件夹就是插件 id 拼接出来的路径
        var installFolder = Path.Join(_storageDirectory.FullName, id);

        if (Directory.Exists(installFolder))
        {
            var configurationFile = new FileInfo(Path.Join(installFolder, ConfigurationFileName));
            if (configurationFile.Exists)
            {
                var configurationDictionary = await ToConfigurationDictionaryAsync(configurationFile);

                foreach (var (key, value) in configurationDictionary)
                {
                    var currentValue = _mainConfigurator.Default[key];
                    if (string.Equals(value, currentValue, StringComparison.Ordinal))
                    {
                        // 这个配置是被此安装的，可以卸载
                        _mainConfigurator.Default[key] = null;

                        // 卸载时，可以将值还原到安装之前的状态。但现在没有这样的需求，没有没有实现
                    }
                    else
                    {
                        // 证明这个值被其他逻辑更改了，这时就不会删掉
                    }
                }
            }

            // 删除此文件夹。这是一个单层文件夹，理论上可以删除
            Directory.Delete(installFolder, true);
        }
    }

    private bool CheckInstalled(string installFolder)
    {
        var file = Path.Join(installFolder, ManifestFileName);
        return File.Exists(file);
    }

    public const string ManifestFileName = "Manifest.coin";
    public const string ConfigurationFileName = "Configuration.coin";

    private static async Task<MemoryConfigurationRepo> ToMemoryConfigurationRepoAsync(FileInfo file)
    {
        var dictionary = await ToConfigurationDictionaryAsync(file);
        var memoryConfigurationRepo = new MemoryConfigurationRepo(dictionary);
        return memoryConfigurationRepo;
    }

    private static async Task<Dictionary<string, string>> ToConfigurationDictionaryAsync(FileInfo file)
    {
        await using var fileStream = file.OpenRead();
        using var streamReader = new StreamReader(fileStream);

        var text = await streamReader.ReadToEndAsync();
        var dictionary = CoinConfigurationSerializer.Deserialize(text);
        return dictionary;
    }
}