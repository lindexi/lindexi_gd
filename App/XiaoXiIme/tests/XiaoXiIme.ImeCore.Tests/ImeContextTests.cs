using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class ImeContextTests
{
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
        var context = CreateContext(new InMemoryImeDictionary(
        [
            new ImeCandidate("按", "an", 100),
            new ImeCandidate("拿", "na", 100),
        ]));
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
        var context = CreateContext(new InMemoryImeDictionary(
        [
            new ImeCandidate("按", "an", 100),
            new ImeCandidate("拿", "na", 100),
        ]));
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
        var context = CreateContext(new InMemoryImeDictionary(
        [
            new ImeCandidate("安", "an", 100),
            new ImeCandidate("拿", "na", 100),
        ]));
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
        Assert.Empty(result.Snapshot.Candidates);
    }

    [Fact]
    public void ProcessKey_BackspaceNormalizesSelectionAfterCandidateRefresh()
    {
        var context = CreateContext(new InMemoryImeDictionary(
        [
            new ImeCandidate("啊", "a", 100),
            new ImeCandidate("阿", "a", 90),
            new ImeCandidate("安", "an", 100),
        ]));
        context.ProcessKey(ImeKey.FromCharacter('a'));
        context.ProcessKey(ImeKey.NextCandidate());

        var result = context.ProcessKey(ImeKey.FromCharacter('n'));
        var backspaceResult = context.ProcessKey(new ImeKey(ImeKeyKind.Backspace));

        Assert.Equal(0, result.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, backspaceResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(0, backspaceResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(2, backspaceResult.Snapshot.CandidateWindow.PageSize);
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

    private static ImeContext CreateContext()
    {
        return CreateContext(new InMemoryImeDictionary(
        [
            new ImeCandidate("你", "ni", 100),
            new ImeCandidate("呢", "ni", 90),
        ]));
    }

    private static ImeContext CreateContext(InMemoryImeDictionary dictionary)
    {
        return new ImeContext(dictionary);
    }

    private static ImeContext CreatePagedContext()
    {
        return CreateContext(new InMemoryImeDictionary(Enumerable.Range(0, 12)
            .Select(index => new ImeCandidate($"候选{index}", "a", 100 - index))));
    }
}

