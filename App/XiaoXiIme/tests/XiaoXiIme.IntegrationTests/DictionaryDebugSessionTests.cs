using Avalonia.Input;
using XiaoXiIme.Dictionary;
using XiaoXiIme.ImeUi.Avalonia;

namespace XiaoXiIme.IntegrationTests;

[Collection(nameof(ProductionDictionaryCollection))]
public class DictionaryDebugSessionTests(ProductionDictionaryFixture fixture)
{
    [Theory]
    [InlineData("fullPinyin", "nihao")]
    [InlineData("xiaoheDoublePinyin", "nihc")]
    public void ProcessKey_WhenSpaceCommitsThenOutputContainsDictionaryCandidate(string scheme, string input)
    {
        var session = CreateSession(scheme);
        Type(session, input);
        session.ProcessKey(Key.Space);
        Assert.Equal("你好", session.CommittedText);
    }

    [Fact]
    public void ProcessKey_WhenTypingThenOutputRemainsEmpty()
    {
        var session = CreateSession();
        Type(session, "nihao");
        Assert.Empty(session.CommittedText);
    }

    [Fact]
    public void ProcessKey_WhenNumberSelectsThenCommitsDisplayedCandidate()
    {
        var session = CreateSession();
        Type(session, "nihao");
        var expected = session.Candidates.Candidates[1].Text;
        session.ProcessKey(Key.D2);
        Assert.Equal(expected, session.CommittedText);
    }

    [Fact]
    public void ProcessKey_WhenPageDownThenShowsNextPage()
    {
        var session = CreateSession();
        Type(session, "ni");
        session.ProcessKey(Key.PageDown);
        Assert.Equal(2, session.Candidates.CurrentPage);
    }

    [Fact]
    public void CancelComposition_WhenComposingThenHidesCandidates()
    {
        var session = CreateSession();
        Type(session, "nihao");
        session.CancelComposition();
        Assert.False(session.Candidates.IsVisible);
    }

    [Theory]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Meta)]
    public void ProcessKey_WhenShortcutThenDoesNotStartComposition(KeyModifiers modifiers)
    {
        var session = CreateSession();
        Assert.False(session.ProcessKey(Key.A, modifiers));
    }

    [Fact]
    public void ProcessKey_WhenBackspaceThenUpdatesComposition()
    {
        var session = CreateSession();
        Type(session, "nihao");
        session.ProcessKey(Key.Back);
        Assert.Equal("niha", session.Candidates.CompositionText);
    }

    private DictionaryDebugSession CreateSession(string scheme = "fullPinyin") => new(
        DictionaryPackageLoader.Load(DictionaryPackageLocations.ResolvePackageDirectory(
            inputScheme: scheme, baseDirectory: fixture.HostRoot)));

    private static void Type(DictionaryDebugSession session, string input)
    {
        foreach (var character in input)
            session.ProcessKey((Key)((int)Key.A + character - 'a'));
    }
}
