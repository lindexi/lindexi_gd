using System.Diagnostics;
using System.Text.Json;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class ProductionIpcPerformanceTests(ProductionDictionaryFixture fixture)
{
    [Theory]
    [InlineData("fullPinyin", "nihao")]
    [InlineData("xiaoheDoublePinyin", "nihc")]
    public async Task ProductionDirectAndIpcQueriesStayWithinExistingRequestBudget(string scheme, string input)
    {
        var trace = new List<string>();
        var evidence = Path.Combine(AppContext.BaseDirectory, $"production-ipc-performance-{scheme}.txt");
        var watch = Stopwatch.StartNew();

        void Record(string stage)
        {
            trace.Add($"{stage}={watch.Elapsed.TotalMilliseconds:F1}ms");
            File.WriteAllLines(evidence, trace);
            watch.Restart();
        }

        var dictionary = DictionaryPackageLoader.Load
        (
            DictionaryPackageLocations.ResolvePackageDirectory
            (
                inputScheme: scheme, baseDirectory: fixture.HostRoot
            )
        );
        Record("load");
        _ = dictionary.GetLexicon();
        Record("canonical");
        var context = new ImeContext(dictionary);
        Record("context/index");
        var cold = context.QueryInterpretations(input);
        Record("cold-query");
        var session = context.PrepareDictionaryChange(dictionary).PrepareFuzzyRulesChange([]);
        Record("session/create");
        var directTimes = new List<TimeSpan>();
        foreach (var character in input)
        {
            watch.Restart();
            var result = session.ProcessKey(ImeKey.FromCharacter(character));
            directTimes.Add(watch.Elapsed);
            Record($"direct/{character}/decode");
            _ = JsonSerializer.Serialize(result);
            Record($"direct/{character}/serialization");
        }

        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Performance_{Guid.NewGuid():N}");
        watch.Restart();
        using var host = fixture.CreateHost(options, scheme);
        Record("host/construct");
        host.Start();
        using var client = new XiaoXiImeIpcClient(options);
        await client.ConnectAsync();
        Record("ipc/connect");
        try
        {
            foreach (var character in input)
            {
                watch.Restart();
                await client.ProcessKeyAsync(ImeKey.FromCharacter(character));
                Record($"ipc/{character}");
            }
        }
        catch (TimeoutException)
        {
            Record("ipc/timeout");
            throw;
        }

        Assert.All
        (
            directTimes, elapsed => Assert.True
            (
                elapsed < options.EffectiveRequestTimeout,
                string.Join(Environment.NewLine, trace)
            )
        );
    }
}