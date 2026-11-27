using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class ImeContextTests
{
    [Theory]
    [InlineData(ImeKeyKind.Space)]
    [InlineData(ImeKeyKind.CandidateSelection)]
    public void WhenAnotherSessionLearnsThenKeyboardSelectionCommitsAndEndsComposition(ImeKeyKind kind)
    {
        var dictionary = new UserDictionary(CanonicalDictionaryFixture.Create([new ImeCandidate("你", "ni", 100)]));
        var first = new ImeContext(dictionary, learning => dictionary.Learn(learning));
        var second = new ImeContext(dictionary, learning => dictionary.Learn(learning));
        first.SetComposition("ni");
        second.SetComposition("ni");
        second.ProcessKey(new ImeKey(ImeKeyKind.Space));

        var result = first.ProcessKey(new ImeKey(kind, CandidateIndex: 0));

        Assert.Equal
        (
            (true, "你", false, ""),
            (result.Handled, result.CommitText, result.Snapshot.IsComposing, result.Snapshot.Composition.Reading)
        );
    }

    [Fact]
    public void ProcessKey_AddsLettersAndRefreshesCandidates()
    {
        var context = CreateContext();

        context.ProcessKey(ImeKey.FromCharacter('n'));
        var result = context.ProcessKey(ImeKey.FromCharacter('i'));

        Assert.True(result.Handled);
        Assert.Null(result.CommitText);
        Assert.True(result.Snapshot.IsComposing);
        Assert.Equal("ni", result.Snapshot.Composition.Reading);
        Assert.Equal("你", result.Snapshot.Candidates[0].Text);
        Assert.Equal(0, result.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, result.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(2, result.Snapshot.CandidateWindow.PageSize);
        Assert.Equal(2, result.Snapshot.Composition.CaretIndex);
        Assert.Equal(ImeGuidelineLevel.Reading, result.Snapshot.EffectiveGuideline.Level);
        Assert.Equal("ni", result.Snapshot.EffectiveGuideline.Text);
    }

    [Fact]
    public void ProcessKey_WhenInputIsSingleSyllablePrefixThenExcludesExtraSyllables()
    {
        var context = CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("你", "ni", 100),
                    new ImeCandidate("你好", "ni hao", 200),
                ]
            )
        );

        var result = context.ProcessKey(ImeKey.FromCharacter('n'));

        Assert.Collection
        (
            result.Snapshot.Candidates,
            candidate => Assert.Equal("你", candidate.Text)
        );
    }

    [Fact]
    public void ProcessKey_WhenContinuousInputCompletesPhraseThenExactPhraseIsFirst()
    {
        var context = CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("你好", "ni hao", 200),
                    new ImeCandidate("你好啊", "ni hao a", 300),
                ]
            )
        );
        ImeProcessResult result = default!;

        foreach (var character in "nihao")
        {
            result = context.ProcessKey(ImeKey.FromCharacter(character));
        }

        Assert.Equal("你好", result.Snapshot.Candidates[0].Text);
    }

    [Fact]
    public void ProcessKey_SpaceCommitsSelectedCandidate()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('i'));
        context.ProcessKey(ImeKey.NextCandidate());

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.True(result.Handled);
        Assert.Equal("呢", result.CommitText);
        Assert.False(result.Snapshot.IsComposing);
    }

    [Fact]
    public void ProcessKey_WhenCandidateCommittedThenReportsCandidateAndOriginalInputForLearning()
    {
        ImeDictionaryLearning? learning = null;
        var context = new ImeContext
        (
            CanonicalDictionaryFixture.Create([new ImeCandidate("你", "ni", 100)]),
            value => learning = value
        );
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('i'));

        context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Equal
        (
            (new ImeCandidate("你", "ni", 100), "ni", new ImeConfirmedSegment("你", "ni", 0, 2, 0, 1)),
            (learning?.Candidate, learning?.OriginalInput, Assert.Single(learning!.Segments))
        );
    }

    [Fact]
    public void ProcessKey_WhenReadingCommittedThenDoesNotReportLearning()
    {
        var learningCount = 0;
        var context = new ImeContext
        (
            CanonicalDictionaryFixture.Create([new ImeCandidate("你", "ni", 100)]),
            _ => learningCount++
        );
        context.ProcessKey(ImeKey.FromCharacter('x'));

        context.ProcessKey(new ImeKey(ImeKeyKind.Enter));

        Assert.Equal(0, learningCount);
    }

    [Fact]
    public void ProcessKey_WhenCompiledPackageContainsCandidateThenSpaceCommitsIt()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ImeCore.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            [new PhoneticDictionaryEntry("你", "ni", 100)],
            packagePath,
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme }
        );
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));
        context.ProcessKey(ImeKey.FromCharacter('n'));
        var composition = context.ProcessKey(ImeKey.FromCharacter('i'));

        var commit = context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Null(composition.CommitText);
        Assert.Equal(("你", false), (commit.CommitText, commit.Snapshot.IsComposing));
    }

    [Fact]
    public void ProcessKey_WhenCompiledXiaohePackageContainsProjectTermThenProjectedKeysCommit()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ImeCore.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile
        (
            XiaoXiImeProjectTerms.Parse(),
            packagePath,
            new DictionaryPackageParameters { InputScheme = DictionaryPackageLocations.XiaoheDoublePinyinInputScheme }
        );
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));
        ImeProcessResult result = default!;

        foreach (var character in "xnxiaimuyi")
        {
            result = context.ProcessKey(ImeKey.FromCharacter(character));
        }

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.Equal("XiaoXiIme", context.ProcessKey(new ImeKey(ImeKeyKind.Space)).CommitText);
    }

    [Fact]
    public void ProcessKey_WhenCompiledFullPinyinPackageContainsProjectTermThenCanonicalKeysCommit()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ImeCore.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(XiaoXiImeProjectTerms.Parse(), packagePath);
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));
        ImeProcessResult result = default!;

        foreach (var character in "xiaoxiaimuyi")
        {
            result = context.ProcessKey(ImeKey.FromCharacter(character));
        }

        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.Equal("XiaoXiIme", context.ProcessKey(new ImeKey(ImeKeyKind.Space)).CommitText);
    }

    [Fact]
    public void SetComposition_WhenCanonicalProjectTermIsReplacedThenDoesNotAutoCommit()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ImeCore.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(XiaoXiImeProjectTerms.Parse(), packagePath);
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));

        var result = context.SetComposition("xiaoxiaimuyi");
        var replacement = context.SetComposition("xiaoxi");
        var cleared = context.SetComposition(string.Empty);

        Assert.True(result.Handled);
        Assert.Null(result.CommitText);
        Assert.Equal("xiaoxiaimuyi", result.Snapshot.Composition.Reading);
        Assert.Equal("XiaoXiIme", result.Snapshot.Candidates[0].Text);
        Assert.True(replacement.Handled);
        Assert.Null(replacement.CommitText);
        Assert.Equal("xiaoxi", replacement.Snapshot.Composition.Reading);
        Assert.Contains(replacement.Snapshot.Candidates, candidate => candidate.Text == "小希");
        Assert.True(cleared.Handled);
        Assert.False(cleared.Snapshot.IsComposing);
        Assert.Empty(cleared.Snapshot.Candidates);
    }

    [Fact]
    public void SetComposition_WhenInputContainsInvalidCharactersThenLeavesCurrentComposition()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var result = context.SetComposition("ni hao");

        Assert.False(result.Handled);
        Assert.Equal("n", result.Snapshot.Composition.Reading);
        Assert.True(result.Snapshot.IsComposing);
    }

    [Fact]
    public void QueryConversionList_WhenCanonicalProjectTermThenLeavesCurrentCompositionUnchanged()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ImeCore.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(XiaoXiImeProjectTerms.Parse(), packagePath);
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var candidates = context.QueryConversionList("xiaoxiaimuyi");
        var snapshot = context.Snapshot;

        Assert.Equal("XiaoXiIme", candidates[0].Text);
        Assert.Equal("n", snapshot.Composition.Reading);
        Assert.True(snapshot.IsComposing);
        Assert.Empty(context.QueryConversionList("ni hao"));
    }

    [Fact]
    public void QueryReverseConversionList_WhenCanonicalProjectTermThenLeavesCurrentCompositionUnchanged()
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "XiaoXiIme.ImeCore.Tests", Guid.NewGuid().ToString("N"));
        DictionaryPackageCompiler.Compile(XiaoXiImeProjectTerms.Parse(), packagePath);
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var candidates = context.QueryReverseConversionList("XiaoXiIme");
        var snapshot = context.Snapshot;

        Assert.Equal("xiao xi ai mu yi", candidates[0].Reading);
        Assert.Equal("XiaoXiIme", candidates[0].Text);
        Assert.Equal("n", snapshot.Composition.Reading);
        Assert.True(snapshot.IsComposing);
        Assert.Empty(context.QueryReverseConversionList("missing"));
    }

    [Fact]
    public void RegisterWord_WhenUserDictionaryThenAddsWordWithoutChangingComposition()
    {
        var dictionary = new UserDictionary
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("你", "ni", 100),
                ]
            )
        );
        var context = new ImeContext(dictionary);
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var registered = context.RegisterWord("zidingyi", "自定义");
        var snapshot = context.Snapshot;
        var entries = context.EnumerateRegisterWords("zidingyi");

        Assert.True(registered);
        Assert.Equal("n", snapshot.Composition.Reading);
        Assert.True(snapshot.IsComposing);
        Assert.Equal("自定义", entries[0].Text);
        Assert.Equal("zidingyi", entries[0].Reading);
        Assert.Equal("自定义", dictionary.Query("zidingyi")[0].Text);
    }

    [Fact]
    public void UnregisterWord_WhenUserWordExistsThenRemovesItWithoutChangingComposition()
    {
        var dictionary = new UserDictionary
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("你", "ni", 100),
                ]
            )
        );
        var context = new ImeContext(dictionary);
        context.RegisterWord("zidingyi", "自定义");
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var unregistered = context.UnregisterWord("zidingyi", "自定义");
        var snapshot = context.Snapshot;

        Assert.True(unregistered);
        Assert.Equal("n", snapshot.Composition.Reading);
        Assert.True(snapshot.IsComposing);
        Assert.Empty(context.EnumerateRegisterWords());
        Assert.Empty(dictionary.Query("zidingyi"));
    }

    [Fact]
    public void RegisterWord_WhenDictionaryIsNotUserDictionaryThenReturnsFalse()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var registered = context.RegisterWord("zidingyi", "自定义");

        Assert.False(registered);
        Assert.Equal("n", context.Snapshot.Composition.Reading);
        Assert.Empty(context.EnumerateRegisterWords());
    }

    [Fact]
    public void ProcessKey_SelectCandidateCommitsCandidateInCurrentPage()
    {
        var context = CreatePagedContext();
        context.ProcessKey(ImeKey.FromCharacter('a'));
        context.ProcessKey(ImeKey.NextCandidatePage());

        var result = context.ProcessKey(ImeKey.SelectCandidate(1));

        Assert.True(result.Handled);
        Assert.Equal("候选10", result.CommitText);
        Assert.False(result.Snapshot.IsComposing);
    }

    [Fact]
    public void ProcessKey_SelectCandidateOutsideCurrentPageDoesNotCommit()
    {
        var context = CreatePagedContext();
        context.ProcessKey(ImeKey.FromCharacter('a'));
        context.ProcessKey(ImeKey.NextCandidatePage());

        var result = context.ProcessKey(ImeKey.SelectCandidate(4));

        Assert.True(result.Handled);
        Assert.Null(result.CommitText);
        Assert.True(result.Snapshot.IsComposing);
        Assert.Equal(9, result.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(3, result.Snapshot.CandidateWindow.PageSize);
    }

    [Fact]
    public void ProcessKey_FirstAndLastCandidateMoveSelectionToBoundaries()
    {
        var context = CreatePagedContext();
        context.ProcessKey(ImeKey.FromCharacter('a'));

        var lastResult = context.ProcessKey(ImeKey.LastCandidate());
        var firstResult = context.ProcessKey(ImeKey.FirstCandidate());

        Assert.True(lastResult.Handled);
        Assert.Equal(11, lastResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(9, lastResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(3, lastResult.Snapshot.CandidateWindow.PageSize);
        Assert.True(firstResult.Handled);
        Assert.Equal(0, firstResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, firstResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(9, firstResult.Snapshot.CandidateWindow.PageSize);
    }

    [Fact]
    public void ProcessKey_FirstAndLastCandidateWithoutCandidatesReturnUnhandled()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('x'));

        var firstResult = context.ProcessKey(ImeKey.FirstCandidate());
        var lastResult = context.ProcessKey(ImeKey.LastCandidate());

        Assert.False(firstResult.Handled);
        Assert.False(lastResult.Handled);
        Assert.True(lastResult.Snapshot.IsComposing);
        Assert.Empty(lastResult.Snapshot.Candidates);
    }

    [Fact]
    public void ProcessKey_DownAndUpMoveSelectionWithinBounds()
    {
        var context = CreatePagedContext();
        context.ProcessKey(ImeKey.FromCharacter('a'));

        var downResult = context.ProcessKey(ImeKey.NextCandidate());
        var upResult = context.ProcessKey(ImeKey.PreviousCandidate());

        Assert.True(downResult.Handled);
        Assert.Equal(1, downResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, downResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(9, downResult.Snapshot.CandidateWindow.PageSize);
        Assert.True(upResult.Handled);
        Assert.Equal(0, upResult.Snapshot.CandidateWindow.Selection);
    }

    [Fact]
    public void ProcessKey_SelectionNavigationClampsAtBoundaries()
    {
        var context = CreatePagedContext();
        context.ProcessKey(ImeKey.FromCharacter('a'));

        var previousResult = context.ProcessKey(ImeKey.PreviousCandidate());
        for (var i = 0; i < 20; i++)
        {
            context.ProcessKey(ImeKey.NextCandidate());
        }

        var nextResult = context.Snapshot;

        Assert.True(previousResult.Handled);
        Assert.Equal(0, previousResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(11, nextResult.CandidateWindow.Selection);
        Assert.Equal(9, nextResult.CandidateWindow.PageStart);
        Assert.Equal(3, nextResult.CandidateWindow.PageSize);
    }

    [Fact]
    public void ProcessKey_PageDownAndPageUpMoveSelectionByPageSize()
    {
        var context = CreatePagedContext();
        context.ProcessKey(ImeKey.FromCharacter('a'));

        var pageDownResult = context.ProcessKey(ImeKey.NextCandidatePage());
        var pageUpResult = context.ProcessKey(ImeKey.PreviousCandidatePage());

        Assert.True(pageDownResult.Handled);
        Assert.Equal(9, pageDownResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(9, pageDownResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(3, pageDownResult.Snapshot.CandidateWindow.PageSize);
        Assert.True(pageUpResult.Handled);
        Assert.Equal(0, pageUpResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, pageUpResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(9, pageUpResult.Snapshot.CandidateWindow.PageSize);
    }

    [Theory]
    [InlineData('=', 0, 9, 9, 3)]
    [InlineData('-', 9, 0, 0, 9)]
    [InlineData('-', 0, 0, 0, 9)]
    [InlineData('=', 9, 11, 9, 3)]
    [InlineData('=', 11, 11, 9, 3)]
    public void WhenCandidatesExistThenPageCharactersMoveSelectionWithoutCommitting
        (char character, int initialSelection, int expectedSelection, int expectedPageStart, int expectedPageSize)
    {
        var context = CreatePagedContext();
        context.SetComposition("a");
        for (var index = 0; index < initialSelection; index++)
        {
            context.ProcessKey(ImeKey.NextCandidate());
        }

        var key = ImeKey.FromCharacter(character);
        var owns = ImeContext.ShouldProcessKey(key, context.Snapshot);
        var result = context.ProcessKey(key);
        var window = result.Snapshot.CandidateWindow;

        Assert.Equal
        (
            (true, true, (string?)null, "a", expectedSelection, expectedPageStart, expectedPageSize),
            (owns, result.Handled, result.CommitText, result.Snapshot.Composition.Reading,
                window.Selection, window.PageStart, window.PageSize)
        );
    }

    [Theory]
    [InlineData('-')]
    [InlineData('=')]
    public void WhenCompositionHasNoCandidatesThenPageCharacterPassesThrough(char character)
    {
        var context = CreateContext();
        context.SetComposition("x");
        Assert.Empty(context.Snapshot.Candidates);

        var key = ImeKey.FromCharacter(character);
        var owns = ImeContext.ShouldProcessKey(key, context.Snapshot);
        var result = context.ProcessKey(key);

        Assert.Equal
        (
            (false, false, (string?)null, "x"),
            (owns, result.Handled, result.CommitText, result.Snapshot.Composition.Reading)
        );
    }

    [Fact]
    public void ProcessKey_NavigationWithoutCandidatesReturnsUnhandled()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('x'));

        var result = context.ProcessKey(ImeKey.NextCandidate());

        Assert.False(result.Handled);
        Assert.True(result.Snapshot.IsComposing);
        Assert.Empty(result.Snapshot.Candidates);
        Assert.Equal(ImeGuidelineLevel.NoCandidate, result.Snapshot.EffectiveGuideline.Level);
        Assert.Equal("无候选：x", result.Snapshot.EffectiveGuideline.Text);
    }

    [Fact]
    public void ProcessKey_MoveCompositionCaretChangesSnapshotCaretIndex()
    {
        var context = CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("按", "an", 100),
                    new ImeCandidate("拿", "na", 100),
                ]
            )
        );
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('a'));

        var leftResult = context.ProcessKey(ImeKey.MoveCompositionCaretLeft());
        var rightResult = context.ProcessKey(ImeKey.MoveCompositionCaretRight());

        Assert.True(leftResult.Handled);
        Assert.Equal(1, leftResult.Snapshot.Composition.CaretIndex);
        Assert.True(rightResult.Handled);
        Assert.Equal(2, rightResult.Snapshot.Composition.CaretIndex);
    }

    [Fact]
    public void ProcessKey_CharacterInsertsAtCompositionCaret()
    {
        var context = CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("按", "an", 100),
                    new ImeCandidate("拿", "na", 100),
                ]
            )
        );
        context.ProcessKey(ImeKey.FromCharacter('a'));
        context.ProcessKey(ImeKey.MoveCompositionCaretLeft());

        var result = context.ProcessKey(ImeKey.FromCharacter('n'));

        Assert.True(result.Handled);
        Assert.Equal("na", result.Snapshot.Composition.Reading);
        Assert.Equal(1, result.Snapshot.Composition.CaretIndex);
        Assert.Equal("拿", result.Snapshot.Candidates[0].Text);
    }

    [Fact]
    public void ProcessKey_BackspaceRemovesCharacterBeforeCompositionCaret()
    {
        var context = CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("安", "an", 100),
                    new ImeCandidate("拿", "na", 100),
                ]
            )
        );
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('a'));
        context.ProcessKey(ImeKey.MoveCompositionCaretLeft());

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.True(result.Handled);
        Assert.Equal("a", result.Snapshot.Composition.Reading);
        Assert.Equal(0, result.Snapshot.Composition.CaretIndex);
    }

    [Fact]
    public void ProcessKey_BackspaceAtStartKeepsComposition()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.MoveCompositionCaretLeft());

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.True(result.Handled);
        Assert.True(result.Snapshot.IsComposing);
        Assert.Equal("n", result.Snapshot.Composition.Reading);
        Assert.Equal(0, result.Snapshot.Composition.CaretIndex);
    }

    [Fact]
    public void ProcessKey_SelectCandidateCommitsRequestedCandidate()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('i'));

        var result = context.ProcessKey(ImeKey.SelectCandidate(1));

        Assert.True(result.Handled);
        Assert.Equal("呢", result.CommitText);
        Assert.False(result.Snapshot.IsComposing);
    }

    [Fact]
    public void ProcessKey_BackspaceRemovesLastReadingCharacter()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('i'));

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.True(result.Handled);
        Assert.Equal("n", result.Snapshot.Composition.Reading);
        Assert.Equal(2, result.Snapshot.Candidates.Count);
    }

    [Fact]
    public void ProcessKey_BackspaceNormalizesSelectionAfterCandidateRefresh()
    {
        var context = CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("啊", "a", 100),
                    new ImeCandidate("阿", "a", 90),
                    new ImeCandidate("安", "an", 100),
                ]
            )
        );
        context.ProcessKey(ImeKey.FromCharacter('a'));
        context.ProcessKey(ImeKey.NextCandidate());

        var result = context.ProcessKey(ImeKey.FromCharacter('n'));
        var backspaceResult = context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.Equal(0, result.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, backspaceResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, backspaceResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(3, backspaceResult.Snapshot.CandidateWindow.PageSize);
    }

    [Fact]
    public void ProcessKey_BackspaceClearsCandidateWindowWhenCompositionClears()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.True(result.Handled);
        Assert.False(result.Snapshot.IsComposing);
        Assert.Equal(0, result.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, result.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(0, result.Snapshot.CandidateWindow.PageSize);
    }

    [Fact]
    public void ProcessKey_EnterCommitsReadingWhenNoCandidateSelected()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('x'));

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Enter));

        Assert.True(result.Handled);
        Assert.Equal("x", result.CommitText);
        Assert.False(result.Snapshot.IsComposing);
    }

    [Fact]
    public void ProcessKey_EscapeClearsComposition()
    {
        var context = CreateContext();
        context.ProcessKey(ImeKey.FromCharacter('n'));

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Escape));

        Assert.True(result.Handled);
        Assert.False(result.Snapshot.IsComposing);
        Assert.Empty(result.Snapshot.Candidates);
    }

    [Fact]
    public void ProcessKey_EscapeWithoutCompositionReturnsUnhandled()
    {
        var context = CreateContext();

        var result = context.ProcessKey(new ImeKey(ImeKeyKind.Escape));

        Assert.False(result.Handled);
        Assert.False(result.Snapshot.IsComposing);
    }

    [Fact]
    public void ProcessKey_WhenUppercaseShapeFollowsPhoneticThenFiltersCandidates()
    {
        var context = new ImeContext(new MultiResourceDictionary());
        context.ProcessKey(ImeKey.FromCharacter('n'));
        context.ProcessKey(ImeKey.FromCharacter('i'));

        var result = context.ProcessKey(ImeKey.FromCharacter('A'));

        Assert.Collection
        (
            context.QueryInterpretations("niA").Candidates.Where
            (c =>
                c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Complete)
            ),
            candidate => Assert.Equal("你", candidate.Candidate.Text)
        );
    }

    [Fact]
    public void ProcessKey_WhenUppercaseShapeStartsCompositionThenQueriesSingleCharacters()
    {
        var context = new ImeContext(new MultiResourceDictionary());

        var result = context.ProcessKey(ImeKey.FromCharacter('A'));

        Assert.Equal("A", result.Snapshot.Composition.Reading);
        Assert.Collection(result.Snapshot.Candidates, candidate => Assert.Equal("你", candidate.Text));
    }

    [Fact]
    public void ProcessKey_WhenSlashInputMatchesSymbolThenShowsSymbolCandidates()
    {
        var context = new ImeContext(new MultiResourceDictionary());
        context.ProcessKey(ImeKey.FromCharacter('/'));
        context.ProcessKey(ImeKey.FromCharacter('X'));

        var result = context.ProcessKey(ImeKey.FromCharacter('H'));

        Assert.Equal("/xh", result.Snapshot.Composition.Reading);
        Assert.Collection
        (
            result.Snapshot.Candidates,
            candidate => Assert.Equal("★", candidate.Text),
            candidate => Assert.Equal("☆", candidate.Text)
        );
    }

    [Fact]
    public void ProcessKey_WhenSymbolInputContainsDigitThenDigitExtendsComposition()
    {
        var context = new ImeContext(new MultiResourceDictionary());
        context.ProcessKey(ImeKey.FromCharacter('/'));

        var result = context.ProcessKey(ImeKey.SelectCandidate(0));

        Assert.Equal("/1", result.Snapshot.Composition.Reading);
        Assert.Collection(result.Snapshot.Candidates, candidate => Assert.Equal("一", candidate.Text));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("/xh")]
    public void ProcessKey_WhenNonPhoneticCandidateCommittedThenDoesNotReportLearning(string input)
    {
        var learningCount = 0;
        var context = new ImeContext(new MultiResourceDictionary(), _ => learningCount++);
        foreach (var character in input)
        {
            context.ProcessKey(ImeKey.FromCharacter(character));
        }

        context.ProcessKey(new ImeKey(ImeKeyKind.Space));

        Assert.Equal(0, learningCount);
    }

    private sealed class MultiResourceDictionary : IImeDictionary, IShapeDictionary, ISymbolDictionary
    {
        public ImeLexicon GetLexicon() => Lexicon;
        public ImeProjectionDefinition Projection => Lexicon.Projection;

        private static readonly ImeLexicon Lexicon = new
        (
            [new("你", "ni", 100), new("呢", "ni", 90)],
            [new("你", "ab", "fixture")], ImeProjectionDefinition.FullPinyin, 1
        );

        private static readonly IReadOnlyList<ImeCandidate> PhoneticCandidates =
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("呢", "ni", 90),
        ];

        public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query)
        {
            return query.Input.Equals("ni", StringComparison.OrdinalIgnoreCase)
                ? PhoneticCandidates.Take(query.MaxCount).ToArray()
                : [];
        }

        public IReadOnlyList<ShapeDictionaryEntry> QueryShape(string shapeCode, int maxCount = 9)
        {
            return shapeCode.Equals("a", StringComparison.OrdinalIgnoreCase)
                ? [new ShapeDictionaryEntry("你", "ab", "亻尔")]
                : [];
        }

        public IReadOnlyList<ImeCandidate> FilterByShape
        (
            IReadOnlyList<ImeCandidate> candidates,
            string shapeCode,
            int maxCount = 9
        )
        {
            return shapeCode.Equals("a", StringComparison.OrdinalIgnoreCase)
                ? candidates.Where(candidate => candidate.Text == "你").Take(maxCount).ToArray()
                : [];
        }

        public IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9)
        {
            return input.ToLowerInvariant() switch
            {
                "/xh" => [new ImeCandidate("★", input), new ImeCandidate("☆", input)],
                "/1" => [new ImeCandidate("一", input)],
                _ => [],
            };
        }
    }

    private static ImeContext CreateContext()
    {
        return CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                [
                    new ImeCandidate("你", "ni", 100),
                    new ImeCandidate("呢", "ni", 90),
                ]
            )
        );
    }

    private static ImeContext CreateContext(InMemoryImeDictionary dictionary)
    {
        return new ImeContext(dictionary);
    }

    private static ImeContext CreatePagedContext()
    {
        return CreateContext
        (
            CanonicalDictionaryFixture.Create
            (
                Enumerable.Range(0, 12)
                    .Select(index => new ImeCandidate($"候选{index}", "a", 100 - index))
            )
        );
    }
}