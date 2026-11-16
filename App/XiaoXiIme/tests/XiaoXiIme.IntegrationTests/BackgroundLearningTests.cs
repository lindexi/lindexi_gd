using System.Diagnostics;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;
using XiaoXiIme.ImeHost;

namespace XiaoXiIme.IntegrationTests;

public class BackgroundLearningTests
{
    private static UserDictionary CreateDictionary() => new(InMemoryImeDictionary.CreateDefault());
    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    [Fact]
    public async Task WhenPersistenceIsPausedThenQueueDropsOldestWithoutBlockingProducer()
    {
        var entered = NewSignal();
        using var worker = new UserDictionaryLearningCoordinator
        (
            CreateDictionary(), async (_, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            }
        );
        worker.TryEnqueue("你", "ni");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        for (var i = 0; i < UserDictionaryLearningCoordinator.Capacity + 17; i++)
        {
            worker.TryEnqueue("你", "ni");
        }

        Assert.Equal(17, worker.Statistics.Dropped);
    }

    [Fact]
    public async Task WhenDuplicateContributionsArriveThenBatchMergesAndEventuallyLearns()
    {
        var dictionary = CreateDictionary();
        var saved = new TaskCompletionSource<IReadOnlyList<UserDictionaryEntry>>
            (TaskCreationOptions.RunContinuationsAsynchronously);
        using var worker = new UserDictionaryLearningCoordinator
        (
            dictionary, (entries, _) =>
            {
                saved.TrySetResult(entries);
                return Task.CompletedTask;
            }
        );
        for (var i = 0; i < 32; i++)
        {
            worker.TryEnqueue("你", "ni");
        }

        var entries = await saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(32, entries.Single(e => e.Text == "你").SelectionCount);
        Assert.Equal(31, worker.Statistics.Merged);
    }

    [Fact]
    public async Task WhenSaveFailsThenFailureIsObservableAndInputProducerStillWorks()
    {
        var failed = NewSignal();
        using var worker = new UserDictionaryLearningCoordinator
        (
            CreateDictionary(), (_, _) =>
            {
                failed.TrySetResult();
                throw new IOException("injected-save-failure");
            }
        );
        worker.TryEnqueue("你", "ni");
        await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        worker.Dispose();
        Assert.Contains("injected-save-failure", worker.LastError);
    }

    [Fact]
    public async Task WhenPersistenceWaitsForCancellationThenShutdownHasFixedBudget()
    {
        var entered = NewSignal();
        var cancelled = NewSignal();
        var worker = new UserDictionaryLearningCoordinator
        (
            CreateDictionary(), async (_, token) =>
            {
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                finally
                {
                    cancelled.TrySetResult();
                }
            }
        );
        worker.TryEnqueue("你", "ni");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var watch = Stopwatch.StartNew();
        worker.Dispose();
        watch.Stop();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.InRange(watch.ElapsedMilliseconds, 0, 500);
        Assert.False(worker.TryEnqueue("你", "ni"));
    }

    [Fact]
    public void WhenFrequencyIsPublishedThenSpellingTopologyAndOldSnapshotRemainStable()
    {
        var dictionary = CreateDictionary();
        dictionary.EnableBackgroundPublication(l => ImeContext.PrepareLexicon(l, []));
        var before = dictionary.GetLexicon();
        dictionary.LearnBatch([new("你", "ni", 4)]);
        Assert.Same(before, dictionary.GetLexicon());
        dictionary.PrepareAndPublish();
        var after = dictionary.GetLexicon();
        Assert.Same(before.Entries, after.Entries);
        Assert.Equal(4, after.UserFrequencies[("你", "ni")]);
        Assert.Empty(before.UserFrequencies);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhenAnotherSessionLearnsThenDisplayedCandidateKeepsOriginalTarget(bool explicitId)
    {
        var dictionary = CreateDictionary();
        dictionary.EnableBackgroundPublication(l => ImeContext.PrepareLexicon(l, []));
        var context = new ImeContext(dictionary);
        context.SetComposition("ni");
        var candidate = context.QueryInterpretations("ni").Candidates[0];
        var expected = context.Snapshot.Candidates[0].Text;
        dictionary.LearnBatch([new("尼", "ni", 1000)]);
        dictionary.PrepareAndPublish();
        var result = explicitId
            ? context.SelectCandidate(candidate.SelectionId!.Value)
            : context.ProcessKey(new(ImeKeyKind.Space));
        Assert.Equal(expected, result.CommitText);
    }

    [Fact]
    public void WhenSelectionIdComesFromAnotherContextThenItIsRejected()
    {
        var dictionary = CreateDictionary();
        dictionary.EnableBackgroundPublication(l => ImeContext.PrepareLexicon(l, []));
        var first = new ImeContext(dictionary);
        var second = new ImeContext(dictionary);
        first.SetComposition("ni");
        second.SetComposition("ni");
        var id = first.QueryInterpretations("ni").Candidates[0].SelectionId!.Value;
        dictionary.LearnBatch([new("尼", "ni", 1000)]);
        dictionary.PrepareAndPublish();
        Assert.Null(second.SelectCandidate(id).CommitText);
    }

    [Fact]
    public async Task WhenUnexpectedPersistenceFailureOccursThenWorkerObservesIt()
    {
        var entered = NewSignal();
        var worker = new UserDictionaryLearningCoordinator
        (
            CreateDictionary(), (_, _) =>
            {
                entered.TrySetResult();
                throw new System.Security.SecurityException("injected-unexpected-failure");
            }
        );
        worker.TryEnqueue("你", "ni");
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        worker.Dispose();
        Assert.Contains("injected-unexpected-failure", worker.LastError);
    }

    [Fact]
    public async Task WhenPublicationConfigurationChangesConcurrentlyThenLatestPreparationWins()
    {
        var dictionary = CreateDictionary();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var old = Task.Run
        (() => dictionary.EnableBackgroundPublication
            (_ =>
                {
                    entered.Set();
                    if (!release.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException();
                    }
                }
            )
        );
        Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
        var prepared = 0;
        var replacement = Task.Run
        (() => dictionary.EnableBackgroundPublication
            (l =>
                {
                    ImeContext.PrepareLexicon(l, [ImeFuzzyRule.ZhZ]);
                    Interlocked.Increment(ref prepared);
                }
            )
        );
        release.Set();
        await Task.WhenAll(old, replacement);
        dictionary.LearnBatch([new("你", "ni", 1)]);
        dictionary.PrepareAndPublish();
        Assert.Equal(2, prepared);
    }

    [Fact]
    public void WhenCountOverflowsThenBatchSaturates()
    {
        var dictionary = CreateDictionary();
        dictionary.LearnBatch([new("你", "ni", int.MaxValue), new("你", "ni", 1)]);
        Assert.Equal(int.MaxValue, dictionary.GetEntries().Single().SelectionCount);
    }
}