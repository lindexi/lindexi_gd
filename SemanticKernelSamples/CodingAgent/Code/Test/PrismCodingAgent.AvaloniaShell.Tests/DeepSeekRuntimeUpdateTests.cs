using System.Net;
using System.Text.Json;
using AgentLib;
using AgentLib.Coding;
using AgentLib.Coding.Sandboxes;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using Microsoft.Extensions.AI;
using PrismCodingAgent.AvaloniaShell.Abilities;
using PrismCodingAgent.AvaloniaShell.Infrastructure;
using PrismCodingAgent.AvaloniaShell.Services.Settings;
using PrismCodingAgent.AvaloniaShell.Services.WorkTasks;
using PrismCodingAgent.AvaloniaShell.ViewModels;

namespace PrismCodingAgent.AvaloniaShell.Tests;

[AvaloniaTestClass]
public sealed class DeepSeekRuntimeUpdateTests
{
    [TestMethod]
    public async Task SavingSettingsShouldUpdateAllExistingRuntimeFunctions()
    {
        var paths = CreatePaths();
        using var firstHttp = new HttpClient(new OfflineHandler());
        using var secondHttp = new HttpClient(new OfflineHandler());
        var firstSource = new DeepSeekWebToolSource(firstHttp, null);
        var secondSource = new DeepSeekWebToolSource(secondHttp, null);
        var dispatcher = new Dispatcher();
        var first = await CodingWorkTaskRuntimeFactory.InitializeAsync(paths, dispatcher, deepSeekWebToolSource: firstSource);
        var second = await CodingWorkTaskRuntimeFactory.InitializeAsync(paths, dispatcher, deepSeekWebToolSource: secondSource);
        var abilities = new AbilityCatalog(paths.AbilitiesDirectory);
        var firstTask = MainViewModel.CreateRuntimeTask(first, new WorkTaskRecord(Guid.NewGuid(), "First", null, null, null), abilities);
        var secondTask = MainViewModel.CreateRuntimeTask(second, new WorkTaskRecord(Guid.NewGuid(), "Second", null, null, null), abilities);
        await using var main = MainViewModel.Create([firstTask, secondTask], [], first.SettingsService,
            new WorkTaskStore(paths), () => CodingWorkTaskRuntimeFactory.InitializeAsync(paths, dispatcher), abilities);
        await main.InitializeSettingsAsync();
        var settings = main.SettingsViewModel ?? throw new InvalidOperationException();
        var functions = new[] { Search(firstSource), Search(secondSource) };
        var arguments = new AIFunctionArguments { ["query"] = "offline" };
        foreach (var function in functions)
        {
            StringAssert.Contains(Text(await function.InvokeAsync(arguments)), "尚未配置");
        }

        settings.DeepSeekApiKey = "saved-test-key";
        await ((SimpleAsyncCommand)settings.SaveCommand).ExecuteAsync();

        foreach (var function in functions)
        {
            Assert.AreEqual("{\"results\":[]}", Text(await function.InvokeAsync(arguments)));
        }
        settings.DeepSeekApiKey = null;
        await ((SimpleAsyncCommand)settings.SaveCommand).ExecuteAsync();
        foreach (var function in functions)
        {
            StringAssert.Contains(Text(await function.InvokeAsync(arguments)), "尚未配置");
        }
    }

    [TestMethod]
    public async Task SavingServiceShouldUpdateCachedFunctionWithoutShellEvent()
    {
        var paths = CreatePaths();
        using var http = new HttpClient(new OfflineHandler());
        var source = new DeepSeekWebToolSource(http, null);
        var service = new CodingChatSettingsService(paths, new WindowsSandboxToolSource(false, string.Empty, string.Empty), source);
        var function = Search(source);
        var arguments = new AIFunctionArguments { ["query"] = "offline" };
        StringAssert.Contains(Text(await function.InvokeAsync(arguments)), "尚未配置");

        await service.SaveAsync(new AgentApiManagerConfiguration(), new CodingChatShellSettings { DeepSeekApiKey = "saved-test-key" });
        Assert.AreEqual("{\"results\":[]}", Text(await function.InvokeAsync(arguments)));
        await service.SaveAsync(new AgentApiManagerConfiguration
        {
            OpenAIConfigurationList = [new OpenAIProtocolLanguageModelConfiguration("https://api.deepseek.com/v1", "saved-test-key")],
        }, new CodingChatShellSettings());
        Assert.AreEqual("{\"results\":[]}", Text(await function.InvokeAsync(arguments)));
        await service.SaveAsync(new AgentApiManagerConfiguration(), new CodingChatShellSettings());
        StringAssert.Contains(Text(await function.InvokeAsync(arguments)), "尚未配置");
    }

    private static CodingChatRoomPaths CreatePaths()
    {
        var paths = CodingChatRoomPaths.Create(Path.Join(Path.GetTempPath(), "DeepSeekRuntimeTests", Guid.NewGuid().ToString("N")));
        Console.WriteLine(paths.RootDirectory);
        return paths;
    }

    private static AIFunction Search(DeepSeekWebToolSource source) => source.CreateTools(Environment.CurrentDirectory)
        .Cast<AIFunction>().Single(tool => tool.Name == "Search");

    private static string? Text(object? value) => Assert.IsInstanceOfType<JsonElement>(value).GetString();

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("saved-test-key", request.Headers.Authorization?.Parameter);
            Assert.AreEqual("https://api.deepseek.com/v1/tools/search", request.RequestUri?.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"results\":[]}") });
        }
    }

    private sealed class Dispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();
        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();
        public bool CheckAccess() => true;
    }
}
