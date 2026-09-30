using System.Diagnostics;
using System.Globalization;

namespace XiaoXiIme.Dictionary.Tests;

[Collection(DictionaryPerformanceCollection.Name)]
public class DictionaryPerformanceTests
{
    private const int QueryMeasurementCount = 2_000;
    private readonly DictionaryPerformanceFixture _fixture;

    public DictionaryPerformanceTests(DictionaryPerformanceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void WhenPackageStartsThenLoadAndValidationCompletesWithinBudget()
    {
        var stopwatch = Stopwatch.StartNew();

        var dictionary = DictionaryPackageLoader.Load(_fixture.PackagePath);

        stopwatch.Stop();
        GC.KeepAlive(dictionary);
        Assert.True(
            stopwatch.Elapsed <= _fixture.Budget.LoadAndValidateTime,
            $"Package load and validation took {stopwatch.Elapsed.TotalMilliseconds:F2} ms; budget is {_fixture.Budget.LoadAndValidateTime.TotalMilliseconds:F2} ms.");
    }

    [Fact]
    public void WhenPackageLoadsThenManagedMemoryIncreaseStaysWithinBudget()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        var dictionary = DictionaryPackageLoader.Load(_fixture.PackagePath);
        var after = GC.GetTotalMemory(forceFullCollection: true);

        GC.KeepAlive(dictionary);
        var managedMemoryIncrease = Math.Max(0, after - before);
        Assert.True(
            managedMemoryIncrease <= _fixture.Budget.ManagedMemoryBytes,
            $"Managed memory increased by {managedMemoryIncrease / 1024d / 1024d:F2} MiB; budget is {_fixture.Budget.ManagedMemoryBytes / 1024d / 1024d:F2} MiB.");
    }

    [Theory]
    [InlineData(50)]
    [InlineData(95)]
    public void WhenExactQueriesRunThenLatencyPercentileStaysWithinBudget(int percentile)
    {
        var actual = GetPercentile(MeasureQueries(ImeDictionaryMatchMode.Exact, _fixture.ExactQueries), percentile);
        var budget = percentile == 50 ? _fixture.Budget.ExactQueryP50Microseconds : _fixture.Budget.ExactQueryP95Microseconds;

        Assert.True(actual <= budget, $"Exact query P{percentile} was {actual:F2} us; budget is {budget:F2} us.");
    }

    [Theory]
    [InlineData(50)]
    [InlineData(95)]
    public void WhenPrefixQueriesRunThenLatencyPercentileStaysWithinBudget(int percentile)
    {
        var actual = GetPercentile(MeasureQueries(ImeDictionaryMatchMode.ExactAndPrefix, _fixture.PrefixQueries), percentile);
        var budget = percentile == 50 ? _fixture.Budget.PrefixQueryP50Microseconds : _fixture.Budget.PrefixQueryP95Microseconds;

        Assert.True(actual <= budget, $"Prefix query P{percentile} was {actual:F2} us; budget is {budget:F2} us.");
    }

    [Fact]
    public void WhenUserDictionarySavesThenElapsedTimeStaysWithinBudget()
    {
        var path = Path.Combine(_fixture.WorkingDirectory, $"user-dictionary-{Guid.NewGuid():N}.json");
        var entries = _fixture.CreateUserDictionaryEntries();
        var stopwatch = Stopwatch.StartNew();

        UserDictionaryStore.Save(path, entries);

        stopwatch.Stop();
        Assert.True(
            stopwatch.Elapsed <= _fixture.Budget.UserDictionarySaveTime,
            $"User dictionary save took {stopwatch.Elapsed.TotalMilliseconds:F2} ms; budget is {_fixture.Budget.UserDictionarySaveTime.TotalMilliseconds:F2} ms.");
    }

    private double[] MeasureQueries(ImeDictionaryMatchMode matchMode, IReadOnlyList<string> inputs)
    {
        for (var index = 0; index < 200; index++)
        {
            _fixture.Dictionary.Query(new ImeDictionaryQuery(inputs[index % inputs.Count], MatchMode: matchMode));
        }

        var elapsedMicroseconds = new double[QueryMeasurementCount];
        for (var index = 0; index < elapsedMicroseconds.Length; index++)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            _fixture.Dictionary.Query(new ImeDictionaryQuery(inputs[index % inputs.Count], MatchMode: matchMode));
            elapsedMicroseconds[index] = Stopwatch.GetElapsedTime(startTimestamp).TotalMicroseconds;
        }

        Array.Sort(elapsedMicroseconds);
        return elapsedMicroseconds;
    }

    private static double GetPercentile(IReadOnlyList<double> sortedValues, int percentile)
    {
        var index = (int)Math.Ceiling(percentile / 100d * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DictionaryPerformanceCollection : ICollectionFixture<DictionaryPerformanceFixture>
{
    public const string Name = "Dictionary performance";
}

public sealed class DictionaryPerformanceFixture
{
    private const int QuerySampleCount = 256;

    public DictionaryPerformanceFixture()
    {
        Budget = DictionaryPerformanceBudget.Load();
        WorkingDirectory = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.PerformanceTests", Guid.NewGuid().ToString("N"));
        PackagePath = Path.Combine(WorkingDirectory, "XiaoXiIme.DictionaryPackage");

        var entries = CreateEntries(Budget.RepresentativeEntryCount);
        DictionaryPackageCompiler.Compile(entries, PackagePath);
        Dictionary = DictionaryPackageLoader.Load(PackagePath);
        ExactQueries = entries
            .Take(QuerySampleCount)
            .Select(entry => entry.Reading.Replace(" ", string.Empty, StringComparison.Ordinal))
            .ToArray();
        PrefixQueries = ExactQueries
            .Select(reading => reading[..Math.Max(1, reading.Length - 2)])
            .ToArray();
    }

    public DictionaryPerformanceBudget Budget { get; }

    public string WorkingDirectory { get; }

    public string PackagePath { get; }

    public CompiledImeDictionary Dictionary { get; }

    public IReadOnlyList<string> ExactQueries { get; }

    public IReadOnlyList<string> PrefixQueries { get; }

    public IReadOnlyList<UserDictionaryEntry> CreateUserDictionaryEntries()
    {
        var count = Math.Min(Budget.RepresentativeEntryCount, 10_000);
        return Enumerable.Range(0, count)
            .Select(index => new UserDictionaryEntry($"用户候选{index:D6}", ExactQueries[index % ExactQueries.Count], index + 1))
            .ToArray();
    }

    private static IReadOnlyList<PhoneticDictionaryEntry> CreateEntries(int count)
    {
        var syllables = new[]
        {
            "a", "ai", "an", "ang", "ao", "ba", "bai", "ban",
            "bang", "bao", "bei", "ben", "bi", "bian", "biao", "bie",
            "bin", "bing", "bo", "bu", "ca", "cai", "can", "cang",
            "cao", "ce", "cen", "ceng", "cha", "chai", "chan", "chang",
        };
        var entries = new PhoneticDictionaryEntry[count];
        for (var index = 0; index < count; index++)
        {
            var value = index;
            var first = syllables[value % syllables.Length];
            value /= syllables.Length;
            var second = syllables[value % syllables.Length];
            value /= syllables.Length;
            var third = syllables[value % syllables.Length];
            value /= syllables.Length;
            var fourth = syllables[value % syllables.Length];
            entries[index] = new PhoneticDictionaryEntry($"候选{index:D6}", $"{first} {second} {third} {fourth}", count - index);
        }

        return entries;
    }
}

public sealed record DictionaryPerformanceBudget(
    int RepresentativeEntryCount,
    TimeSpan LoadAndValidateTime,
    long ManagedMemoryBytes,
    double ExactQueryP50Microseconds,
    double ExactQueryP95Microseconds,
    double PrefixQueryP50Microseconds,
    double PrefixQueryP95Microseconds,
    TimeSpan UserDictionarySaveTime)
{
    public static DictionaryPerformanceBudget Load()
    {
        return new DictionaryPerformanceBudget(
            ReadPositiveInt32("XIAOXIIME_PERF_ENTRY_COUNT", 25_000),
            TimeSpan.FromMilliseconds(ReadPositiveDouble("XIAOXIIME_PERF_LOAD_MS", 5_000)),
            checked((long)(ReadPositiveDouble("XIAOXIIME_PERF_MEMORY_MIB", 512) * 1024 * 1024)),
            ReadPositiveDouble("XIAOXIIME_PERF_EXACT_P50_US", 500),
            ReadPositiveDouble("XIAOXIIME_PERF_EXACT_P95_US", 1_500),
            ReadPositiveDouble("XIAOXIIME_PERF_PREFIX_P50_US", 1_000),
            ReadPositiveDouble("XIAOXIIME_PERF_PREFIX_P95_US", 3_000),
            TimeSpan.FromMilliseconds(ReadPositiveDouble("XIAOXIIME_PERF_USER_SAVE_MS", 5_000)));
    }

    private static int ReadPositiveInt32(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }

    private static double ReadPositiveDouble(string name, double defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}
