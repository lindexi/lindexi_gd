using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

public class CompiledDictionaryCandidateSelectionTests
{
    [Theory]
    [InlineData("fullPinyin", "nihao")]
    [InlineData("xiaoheDoublePinyin", "nihc")]
    public void ProcessKey_WhenCompiledPackageIsUsedThenRanksAndSelectsCandidates(
        string inputScheme,
        string input)
    {
        var packagePath = Path.Combine(
            Path.GetTempPath(),
            "XiaoXiIme.ImeCore.Tests",
            Guid.NewGuid().ToString("N"));
        var entries = Enumerable.Range(0, 12)
            .Select(index => new PhoneticDictionaryEntry($"候选{index:00}", "ni hao", 100))
            .Append(new PhoneticDictionaryEntry("高频前缀", "ni hao a", 1000));
        DictionaryPackageCompiler.Compile(
            entries,
            packagePath,
            new DictionaryPackageParameters { InputScheme = inputScheme });
        var context = new ImeContext(DictionaryPackageLoader.Load(packagePath));

        ImeProcessResult inputResult = default!;
        foreach (var character in input)
        {
            inputResult = context.ProcessKey(ImeKey.FromCharacter(character));
        }

        Assert.Collection(
            inputResult.Snapshot.Candidates,
            candidate => Assert.Equal("候选00", candidate.Text),
            candidate => Assert.Equal("候选01", candidate.Text),
            candidate => Assert.Equal("候选02", candidate.Text),
            candidate => Assert.Equal("候选03", candidate.Text),
            candidate => Assert.Equal("候选04", candidate.Text),
            candidate => Assert.Equal("候选05", candidate.Text),
            candidate => Assert.Equal("候选06", candidate.Text),
            candidate => Assert.Equal("候选07", candidate.Text),
            candidate => Assert.Equal("候选08", candidate.Text),
            candidate => Assert.Equal("候选09", candidate.Text),
            candidate => Assert.Equal("候选10", candidate.Text),
            candidate => Assert.Equal("候选11", candidate.Text),
            candidate => Assert.Equal("高频前缀", candidate.Text));

        var pageResult = context.ProcessKey(ImeKey.NextCandidatePage());
        var commitResult = context.ProcessKey(ImeKey.SelectCandidate(1));

        Assert.Equal(9, pageResult.Snapshot.CandidateWindow.Selection);
        Assert.Equal(9, pageResult.Snapshot.CandidateWindow.PageStart);
        Assert.Equal(4, pageResult.Snapshot.CandidateWindow.PageSize);
        Assert.Equal("候选10", commitResult.CommitText);
        Assert.False(commitResult.Snapshot.IsComposing);
    }
}
