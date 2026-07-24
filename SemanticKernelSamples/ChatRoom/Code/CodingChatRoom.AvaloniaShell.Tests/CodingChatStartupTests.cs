using AgentLib;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using CodingChatRoom.AvaloniaShell.Infrastructure;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class CodingChatStartupTests
{
    [TestMethod(DisplayName = "路径对象应只在指定根目录下计算固定文件和子目录")]
    [Timeout(5000)]
    public void PathsShouldUseOnlyTheSpecifiedRootDirectory()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);

        Assert.AreEqual(Path.GetFullPath(temporaryDirectory.Path), paths.RootDirectory);
        Assert.AreEqual(
            Path.Join(paths.RootDirectory, "AgentApiManagerConfiguration.json"),
            paths.ConfigurationFile.FullName);
        Assert.AreEqual(Path.Join(paths.RootDirectory, "Logs"), paths.LogDirectory);
        Assert.AreEqual(Path.Join(paths.RootDirectory, "Sessions"), paths.SessionDirectory);
    }

    [TestMethod(DisplayName = "目录初始化不应生成配置文件")]
    [Timeout(5000)]
    public void EnsureDirectoriesShouldNotCreateConfigurationFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);

        paths.EnsureDirectories();

        Assert.IsTrue(Directory.Exists(paths.RootDirectory));
        Assert.IsTrue(Directory.Exists(paths.LogDirectory));
        Assert.IsTrue(Directory.Exists(paths.SessionDirectory));
        Assert.IsFalse(paths.ConfigurationFile.Exists);
    }

    [TestMethod(DisplayName = "配置缺失时启动应报告固定完整路径")]
    [Timeout(5000)]
    public async Task MissingConfigurationShouldFailWithFixedFullPath()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);

        FileNotFoundException exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => CodingChatStartup.InitializeAsync(paths, new ImmediateMainThreadDispatcher()));

        StringAssert.Contains(exception.Message, paths.ConfigurationFile.FullName);
        Assert.IsFalse(paths.ConfigurationFile.Exists);
    }

    [TestMethod(DisplayName = "损坏配置时启动应直接失败")]
    [Timeout(5000)]
    public async Task InvalidConfigurationShouldFailWithoutFallback()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);
        paths.EnsureDirectories();
        await File.WriteAllTextAsync(paths.ConfigurationFile.FullName, "{ invalid json");

        await Assert.ThrowsExactlyAsync<System.Text.Json.JsonException>(
            () => CodingChatStartup.InitializeAsync(paths, new ImmediateMainThreadDispatcher()));
    }

    [TestMethod(DisplayName = "没有有效 Key 时启动应报告无可用模型")]
    [Timeout(5000)]
    public async Task ConfigurationWithoutUsableProviderShouldFail()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);
        paths.EnsureDirectories();
        await CreateConfiguration(primaryModel: null, key: string.Empty).SaveToFileAsync(paths.ConfigurationFile);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CodingChatStartup.InitializeAsync(paths, new ImmediateMainThreadDispatcher()));

        StringAssert.Contains(exception.Message, "模型列表");
    }

    [TestMethod(DisplayName = "未知首选模型时启动应保留模型管理器异常")]
    [Timeout(5000)]
    public async Task UnknownPrimaryModelShouldFail()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);
        paths.EnsureDirectories();
        await CreateConfiguration(primaryModel: "missing-model", key: "test-key")
            .SaveToFileAsync(paths.ConfigurationFile);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CodingChatStartup.InitializeAsync(paths, new ImmediateMainThreadDispatcher()));

        StringAssert.Contains(exception.Message, "missing-model");
    }

    [TestMethod(DisplayName = "有效配置应创建使用固定日志和会话目录的运行时")]
    [Timeout(10000)]
    public async Task ValidConfigurationShouldUseExplicitLogAndSessionDirectories()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        CodingChatRoomPaths paths = CodingChatRoomPaths.Create(temporaryDirectory.Path);
        paths.EnsureDirectories();
        await CreateConfiguration(primaryModel: "test-model", key: "test-key")
            .SaveToFileAsync(paths.ConfigurationFile);

        await using CodingChatRuntime runtime = await CodingChatStartup.InitializeAsync(
            paths,
            new ImmediateMainThreadDispatcher());

        Assert.AreEqual(paths.LogDirectory, runtime.ChatLogger.ChatLogFolder);
        Assert.AreEqual("test-provider/test-model", runtime.ModelDisplayName);
        Assert.AreSame(runtime.EndpointManager, runtime.ChatManager.AgentApiEndpointManager);
    }

    private static AgentApiManagerConfiguration CreateConfiguration(string? primaryModel, string key)
    {
        return new AgentApiManagerConfiguration
        {
            PrimaryModel = primaryModel,
            OpenAIConfigurationList =
            [
                new OpenAIProtocolLanguageModelConfiguration("https://example.test/v1", key)
                {
                    ModelDefinitions =
                    [
                        new ModelDefinition
                        {
                            Provider = "test-provider",
                            ModelName = "test-model",
                            Capabilities = new LlmModelCapabilities(),
                        },
                    ],
                },
            ],
        };
    }

    private sealed class ImmediateMainThreadDispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();

        public bool CheckAccess() => true;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Join(
                System.IO.Path.GetTempPath(),
                "CodingChatRoom.Tests",
                Guid.NewGuid().ToString("N"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
