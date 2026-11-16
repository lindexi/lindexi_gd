using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary.Tests;

public class SoundShapeLearningTests
{
    [Fact]
    public void SegmentsLearnCanonicalKeysWithoutInventingSentence()
    {
        var dictionary = Create();
        dictionary.Learn
        (
            new(new ImeCandidate("你好", "ni hao", 40), "niRXhaoNZ")
                { Segments = [new("你", "ni", 0, 4, 0, 1), new("好", "hao", 4, 5, 1, 1)] }
        );

        Assert.Equal
        (
            new[] { new UserDictionaryEntry("好", "hao", 1), new UserDictionaryEntry("你", "ni", 1) },
            dictionary.GetEntries()
        );
    }

    [Fact]
    public void InvalidLaterSegmentDoesNotPartiallyLearn()
    {
        var dictionary = Create();
        Assert.Throws<ArgumentException>
        (() => dictionary.Learn
            (
                new(new ImeCandidate("你好", "ni hao", 40), "niRXhaoNZ")
                    { Segments = [new("你", "ni", 0, 4, 0, 1), new("好", "", 4, 5, 1, 1)] }
            )
        );
    }

    [Fact]
    public void FailedLearningKeepsFrequenciesUnchanged()
    {
        var dictionary = Create();
        try
        {
            dictionary.Learn
            (
                new(new ImeCandidate("你好", "ni hao", 40), "niRXhaoNZ")
                    { Segments = [new("你", "ni", 0, 4, 0, 1), new("好", "", 4, 5, 1, 1)] }
            );
        }
        catch (ArgumentException)
        {
        }

        Assert.Empty(dictionary.GetEntries());
    }

    [Fact]
    public void InvalidLearningKeepsVersionAndExistingFrequencyUnchanged()
    {
        var dictionary = Create();
        dictionary.Learn
        (
            new(new ImeCandidate("你", "ni", 40), "niR")
                { Segments = [new("你", "ni", 0, 3, 0, 1)] }
        );
        var version = dictionary.GetLexicon().Version;
        var error = Record.Exception
        (() => dictionary.Learn
            (
                new(new ImeCandidate("你好", "ni hao", 40), "niRhaoN")
                    { Segments = [new("你", "ni", 0, 3, 0, 1), new("好", "haoN", 3, 4, 1, 1)] }
            )
        );

        Assert.Equal
        (
            (typeof(ArgumentException), version, 1), (error?.GetType(), dictionary.GetLexicon().Version,
                dictionary.GetEntries().Single().SelectionCount)
        );
    }

    [Fact]
    public void RepeatedCanonicalSegmentInOneEventIncrementsTwice()
    {
        var dictionary = Create();
        dictionary.Learn
        (
            new(new ImeCandidate("你你", "ni ni", 40), "niRniR")
                { Segments = [new("你", "ni", 0, 3, 0, 1), new("你", "ni", 3, 3, 1, 1)] }
        );

        Assert.Equal(new[] { new UserDictionaryEntry("你", "ni", 2) }, dictionary.GetEntries());
    }

    [Fact]
    public void UnchangedLexiconReusesImmutableSnapshot()
    {
        var dictionary = Create();
        Assert.Same(dictionary.GetLexicon(), dictionary.GetLexicon());
    }

    [Fact]
    public void LearningReplacesSnapshotWithoutMutatingPreviousSnapshot()
    {
        var dictionary = Create();
        var previous = dictionary.GetLexicon();
        dictionary.Learn(new(new ImeCandidate("你", "ni", 40), "ni"));
        var current = dictionary.GetLexicon();
        Assert.Equal
        (
            (0, 1, previous.Version + 1),
            (previous.Entries.Single(e => e.Text == "你").UserFrequency,
                current.Entries.Single(e => e.Text == "你").UserFrequency, current.Version)
        );
    }

    [Fact]
    public async Task ConcurrentLearningPublishesOnlyWholeImmutableSnapshotsAsync()
    {
        var dictionary = Create();
        var previous = dictionary.GetLexicon();
        var snapshots = new System.Collections.Concurrent.ConcurrentBag<ImeLexicon>();
        snapshots.Add(previous);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writer = Task.Run
        (async () =>
            {
                await start.Task;
                dictionary.Learn
                (
                    new(new ImeCandidate("你好", "ni hao", 40), "nihao")
                        { Segments = [new("你", "ni", 0, 2, 0, 1), new("好", "hao", 2, 3, 1, 1)] }
                );
            }
        );
        var readers = Enumerable.Range(0, 4).Select
        (_ => Task.Run
            (async () =>
                {
                    await start.Task;
                    for (var i = 0; i < 100; i++)
                    {
                        snapshots.Add(dictionary.GetLexicon());
                    }
                }
            )
        ).ToArray();
        start.SetResult();
        await Task.WhenAll(readers.Append(writer));
        snapshots.Add(dictionary.GetLexicon());
        Assert.All
        (
            snapshots, snapshot =>
            {
                var frequencies = snapshot.Entries.Select(e => e.UserFrequency).Distinct().ToArray();
                Assert.Equal(new[] { checked((int)(snapshot.Version - previous.Version)) }, frequencies);
                Assert.True(((ICollection<ImePhoneticEntry>)snapshot.Entries).IsReadOnly);
            }
        );
        Assert.Equal(new[] { 0, 0 }, previous.Entries.Select(e => e.UserFrequency));
        Assert.Equal(new[] { 1, 1 }, dictionary.GetLexicon().Entries.Select(e => e.UserFrequency));
    }

    private static UserDictionary Create() => new
    (
        new InMemoryImeDictionary
        (
            new ImeLexicon
            (
                [new("你", "ni", 40), new("好", "hao", 40)], [], ImeProjectionDefinition.FullPinyin, 1
            )
        )
    );
}