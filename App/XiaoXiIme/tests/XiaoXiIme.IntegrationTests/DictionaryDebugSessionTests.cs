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

    [Fact]
    public void LegacyExperienceNumberSelectionRetainsUnconsumedInput()
    {
        var session = new DictionaryDebugSession
        (
            new InMemoryImeDictionary
            (
                new ImeLexicon
                (
                    [
                        new("比", "bi", 100), new("笔", "bi", 90), new("闭", "bi", 80), new("臂", "bi", 70),
                        new("醋", "cu", 100), new("粗", "cu", 90)
                    ], [], ImeProjectionDefinition.FullPinyin, 1
                )
            )
        );
        Type(session, "bicu");
        session.ProcessKey(Key.D3);
        Assert.Equal
        (
            ("笔", "cu", "醋"),
            (session.CommittedText, session.Candidates.CompositionText, session.Candidates.Candidates[0].Text)
        );
    }

    [Theory]
    [InlineData("bicu")]
    [InlineData("be")]
    public void FullProductionDictionaryHasContinuousInputCandidates(string input)
    {
        var session = CreateSession();
        Type(session, input);
        Assert.NotEmpty(session.Candidates.Candidates);
    }

    [Theory]
    [InlineData(Key.D0, "0")]
    [InlineData(Key.D1, "1")]
    [InlineData(Key.D2, "2")]
    [InlineData(Key.D3, "3")]
    [InlineData(Key.D4, "4")]
    [InlineData(Key.D5, "5")]
    [InlineData(Key.D6, "6")]
    [InlineData(Key.D7, "7")]
    [InlineData(Key.D8, "8")]
    [InlineData(Key.D9, "9")]
    [InlineData(Key.NumPad0, "0")]
    [InlineData(Key.NumPad1, "1")]
    [InlineData(Key.NumPad2, "2")]
    [InlineData(Key.NumPad3, "3")]
    [InlineData(Key.NumPad4, "4")]
    [InlineData(Key.NumPad5, "5")]
    [InlineData(Key.NumPad6, "6")]
    [InlineData(Key.NumPad7, "7")]
    [InlineData(Key.NumPad8, "8")]
    [InlineData(Key.NumPad9, "9")]
    [InlineData(Key.Add, "+")]
    [InlineData(Key.Subtract, "-")]
    [InlineData(Key.Multiply, "*")]
    [InlineData(Key.Decimal, ".")]
    public void ProcessKey_WhenNotComposingThenCommitsDigitOrOperator(Key key, string expected)
    {
        var session = CreateSession();
        session.ProcessKey(key);
        Assert.Equal(expected, session.CommittedText);
    }

    [Theory]
    [InlineData(Key.D0, ")")]
    [InlineData(Key.D1, "!")]
    [InlineData(Key.D2, "@")]
    [InlineData(Key.D3, "#")]
    [InlineData(Key.D4, "$")]
    [InlineData(Key.D5, "%")]
    [InlineData(Key.D6, "^")]
    [InlineData(Key.D7, "&")]
    [InlineData(Key.D8, "*")]
    [InlineData(Key.D9, "(")]
    [InlineData(Key.OemSemicolon, ":")]
    [InlineData(Key.OemQuestion, "?")]
    [InlineData(Key.OemQuotes, "\"")]
    [InlineData(Key.OemComma, "<")]
    [InlineData(Key.OemPeriod, ">")]
    [InlineData(Key.OemMinus, "_")]
    [InlineData(Key.OemPlus, "+")]
    [InlineData(Key.OemOpenBrackets, "{")]
    [InlineData(Key.OemCloseBrackets, "}")]
    [InlineData(Key.OemPipe, "|")]
    [InlineData(Key.OemTilde, "~")]
    public void ProcessKey_WhenShiftedThenCommitsPunctuation(Key key, string expected)
    {
        var session = CreateSession();
        session.ProcessKey(key, KeyModifiers.Shift);
        Assert.Equal(expected, session.CommittedText);
    }

    [Theory]
    [InlineData(Key.OemComma, ",")]
    [InlineData(Key.OemPeriod, ".")]
    [InlineData(Key.OemQuotes, "'")]
    [InlineData(Key.OemSemicolon, ";")]
    [InlineData(Key.OemMinus, "-")]
    [InlineData(Key.OemPlus, "=")]
    [InlineData(Key.OemOpenBrackets, "[")]
    [InlineData(Key.OemCloseBrackets, "]")]
    [InlineData(Key.OemPipe, "\\")]
    [InlineData(Key.OemTilde, "`")]
    public void ProcessKey_WhenOrdinaryPunctuationThenCommitsOnce(Key key, string expected)
    {
        var session = CreateSession();
        session.ProcessKey(key);
        Assert.Equal(expected, session.CommittedText);
    }

    [Theory]
    [InlineData(Key.D2)]
    [InlineData(Key.NumPad2)]
    public void ProcessKey_WhenDigitAfterCandidateThenDoesNotAlsoCommitDigit(Key key)
    {
        var session = CreateSession();
        Type(session, "nihao");
        var expected = session.Candidates.Candidates[1].Text;
        session.ProcessKey(key);
        Assert.Equal(expected, session.CommittedText);
    }

    [Fact]
    public void ProcessKey_WhenPunctuationFollowsCompositionThenCommitsCandidateAndPunctuation()
    {
        var session = CreateSession();
        Type(session, "nihao");
        session.ProcessKey(Key.D1, KeyModifiers.Shift, "!");
        Assert.Equal("你好!", session.CommittedText);
    }

    [Theory]
    [InlineData(Key.D1, KeyModifiers.None, "&")]
    [InlineData(Key.OemPeriod, KeyModifiers.Shift, ":")]
    [InlineData(Key.NumPad0, KeyModifiers.None, ",")]
    public void ProcessKey_WhenLayoutSymbolAvailableThenUsesItInsteadOfPhysicalLabel
        (Key key, KeyModifiers modifiers, string symbol)
    {
        var session = CreateSession();
        session.ProcessKey(key, modifiers, symbol);
        Assert.Equal(symbol, session.CommittedText);
    }

    [Theory]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Meta)]
    public void ProcessKey_WhenShortcutHasSymbolThenDoesNotCommit(KeyModifiers modifiers)
    {
        var session = CreateSession();
        session.ProcessKey(Key.D1, modifiers, "!");
        Assert.Empty(session.CommittedText);
    }

    [Theory]
    [InlineData(Key.OemQuestion)]
    [InlineData(Key.Divide)]
    public void ProcessKey_WhenSlashThenStartsSymbolComposition(Key key)
    {
        var session = CreateSession();
        session.ProcessKey(key);
        session.ProcessKey(Key.D0);
        Assert.Equal("/0", session.Candidates.CompositionText);
    }

    [Fact]
    public void ProcessKey_WhenShiftLetterThenPreservesShapeCase()
    {
        var session = CreateSession();
        Type(session, "ni");
        session.ProcessKey(Key.R, KeyModifiers.Shift);
        Assert.Equal("niR", session.Candidates.CompositionText);
    }

    [Fact]
    public void ProcessKey_WhenSchemeAcceptsSemicolonThenRetainsItInComposition()
    {
        var session = CreateSession("microsoftDoublePinyin");
        Type(session, "y");
        session.ProcessKey(Key.OemSemicolon);
        Assert.Equal("y;", session.Candidates.CompositionText);
    }

    [Fact]
    public void ProcessKey_WhenUnsupportedThenReturnsFalse()
    {
        var session = CreateSession();
        Assert.False(session.ProcessKey(Key.F5));
    }

    private DictionaryDebugSession CreateSession(string scheme = "fullPinyin") => new
    (
        DictionaryPackageLoader.Load
        (
            DictionaryPackageLocations.ResolvePackageDirectory
            (
                inputScheme: scheme, baseDirectory: fixture.HostRoot
            )
        )
    );

    private static void Type(DictionaryDebugSession session, string input)
    {
        foreach (var character in input)
        {
            session.ProcessKey((Key)((int)Key.A + character - 'a'));
        }
    }
}