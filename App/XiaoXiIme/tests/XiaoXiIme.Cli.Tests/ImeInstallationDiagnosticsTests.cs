using XiaoXiIme.Cli;

namespace XiaoXiIme.Cli.Tests;

public sealed class ImeInstallationDiagnosticsTests
{
    [Fact]
    public void ReadPeReturnsMetadataForCurrentExecutable()
    {
        var executablePath = Environment.ProcessPath;
        Assert.False(string.IsNullOrWhiteSpace(executablePath));

        var (diagnostic, imports) = ImeInstallationDiagnostics.ReadPe(executablePath);

        Assert.Null(diagnostic.ParseError);
        Assert.NotEqual("Unknown", diagnostic.Machine);
        Assert.NotEqual("Unknown", diagnostic.Magic);
        Assert.True(diagnostic.HasImportTable);
        Assert.Equal(imports.Count, diagnostic.ImportCount);
        Assert.NotEmpty(imports);
    }

    [Fact]
    public void ReadPeReturnsParseErrorForInvalidImage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"XiaoXiIme-invalid-{Guid.NewGuid():N}.ime");
        try
        {
            File.WriteAllText(path, "not a PE image");

            var (diagnostic, imports) = ImeInstallationDiagnostics.ReadPe(path);

            Assert.NotNull(diagnostic.ParseError);
            Assert.Equal("Unknown", diagnostic.Machine);
            Assert.Empty(imports);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NativeLoadProbeReturnsLoaderErrorForMissingImage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"XiaoXiIme-missing-{Guid.NewGuid():N}.ime");

        var result = NativeImeLoadProbe.Probe(path);

        Assert.False(result.LoadSucceeded);
        Assert.NotEqual(0, result.LoadErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(result.LoadErrorMessage));
        Assert.False(result.AllRequiredExportsFound);
        Assert.All(result.RequiredExports, export => Assert.False(export.Value));
    }

    [Fact]
    public void InstallationVariantPathsSeparateFilenameAndDirectoryVariables()
    {
        var source = Path.Combine("C:\\payload", "native", "XiaoXiIme.ime");
        var systemDirectory = "C:\\Windows\\System32";

        var variants = WindowsImeInstallationVariantProbe.CreateVariantPaths(source, systemDirectory);

        Assert.Equal(3, variants.Count);
        Assert.Equal(("payload-short-name", Path.Combine("C:\\payload", "native", "XIAOXI.IME")), variants[0]);
        Assert.Equal(("system32-original-name", Path.Combine(systemDirectory, "XiaoXiIme.ime")), variants[1]);
        Assert.Equal(("system32-short-name", Path.Combine(systemDirectory, "XIAOXI.IME")), variants[2]);
    }

    [Theory]
    [InlineData("XiaoXi IME", "XiaoXiIme.ime", true)]
    [InlineData("XiaoXi IME Probe [system32-original-name]", "XiaoXiIme.ime", true)]
    [InlineData("XiaoXi IME Probe [system32-short-name]", "XIAOXI.IME", true)]
    [InlineData("Other IME", "other.ime", false)]
    public void InstallerRecognizesOnlyExpectedLayouts(string layoutText, string imeFile, bool expected)
    {
        Assert.Equal(expected, WindowsImeInstaller.IsXiaoXiIme(layoutText, imeFile));
    }

    [Theory]
    [InlineData("XiaoXiIme.ime", true)]
    [InlineData("XIAOXI.IME", true)]
    [InlineData("other.ime", false)]
    public void InstallerRecognizesExpectedSystemImeFiles(string imeFile, bool expected)
    {
        Assert.Equal(expected, WindowsImeInstaller.IsExpectedXiaoXiImeFile(imeFile));
    }
}
