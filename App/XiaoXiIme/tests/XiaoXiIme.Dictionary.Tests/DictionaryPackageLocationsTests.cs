namespace XiaoXiIme.Dictionary.Tests;

public class DictionaryPackageLocationsTests
{
    [Fact]
    public void ResolvePackageDirectory_WhenPathIsExplicitThenUsesThatDirectory()
    {
        var packageDirectory = Path.Combine(Path.GetTempPath(), "explicit-package");

        var resolved = DictionaryPackageLocations.ResolvePackageDirectory(
            packageDirectory,
            DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
            Path.GetTempPath());

        Assert.Equal(Path.GetFullPath(packageDirectory), resolved);
    }

    [Fact]
    public void ResolvePackageDirectory_WhenSchemeIsDefaultThenUsesFullPinyinLayout()
    {
        var hostRoot = Path.Combine(Path.GetTempPath(), "XiaoXiIme.HostRoot");

        var resolved = DictionaryPackageLocations.ResolvePackageDirectory(
            packageDirectory: null,
            inputScheme: DictionaryPackageLocations.FullPinyinInputScheme,
            baseDirectory: hostRoot);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(hostRoot, DictionaryPackageLocations.FullPinyinPackageDirectoryName)),
            resolved);
    }

    [Fact]
    public void ResolvePackageDirectory_WhenSchemeIsXiaoheThenUsesInstalledLayout()
    {
        var hostRoot = Path.Combine(Path.GetTempPath(), "XiaoXiIme.HostRoot");

        var resolved = DictionaryPackageLocations.ResolvePackageDirectory(
            packageDirectory: null,
            inputScheme: DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
            baseDirectory: hostRoot);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(hostRoot, DictionaryPackageLocations.XiaoheDoublePinyinPackageRelativePath)),
            resolved);
    }

    [Theory]
    [InlineData("fullPinyin", "fullPinyin")]
    [InlineData("FULLPINYIN", "fullPinyin")]
    [InlineData("xiaoheDoublePinyin", "xiaoheDoublePinyin")]
    [InlineData("unknown", "fullPinyin")]
    public void ResolveInputScheme_WhenValueIsMissingOrUnknownThenUsesFullPinyin(string requested, string expected)
    {
        Assert.Equal(expected, DictionaryPackageLocations.ResolveInputScheme(requested));
    }

    [Fact]
    public void ResolveInputScheme_WhenEnvironmentVariableIsXiaoheThenUsesXiaohe()
    {
        var previous = Environment.GetEnvironmentVariable(DictionaryPackageLocations.InputSchemeEnvironmentVariableName);
        try
        {
            Environment.SetEnvironmentVariable(
                DictionaryPackageLocations.InputSchemeEnvironmentVariableName,
                DictionaryPackageLocations.XiaoheDoublePinyinInputScheme);

            Assert.Equal(
                DictionaryPackageLocations.XiaoheDoublePinyinInputScheme,
                DictionaryPackageLocations.ResolveInputScheme());
        }
        finally
        {
            Environment.SetEnvironmentVariable(DictionaryPackageLocations.InputSchemeEnvironmentVariableName, previous);
        }
    }
}
