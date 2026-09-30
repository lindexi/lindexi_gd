namespace XiaoXiIme.Dictionary.Tests;

public class LocalDictionaryPackageManagerTests
{
    [Fact]
    public void Update_WhenSourceIsValidThenInstallsQueryablePackage()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "first.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");

        var manifest = LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Update_WhenSourcesAreUnderCommonRootThenManifestUsesStableRelativePaths()
    {
        var root = CreateRoot();
        var phoneticDirectory = Path.Combine(root, "phonetic");
        Directory.CreateDirectory(phoneticDirectory);
        var phoneticPath = WritePhoneticSource(phoneticDirectory, "first.phonetic.tsv", "你\tni\t100");
        var shapeDirectory = Path.Combine(root, "shape");
        Directory.CreateDirectory(shapeDirectory);
        var shapePath = Path.Combine(shapeDirectory, "first.shape.tsv");
        File.WriteAllText(shapePath, "你\trb\t亻尔");

        var manifest = LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [phoneticPath], ShapeSourcePaths = [shapePath] },
            Path.Combine(root, "package"));

        Assert.Equal(["phonetic/first.phonetic.tsv", "shape/first.shape.tsv"], manifest.Sources.Select(source => source.Path));
        Assert.All(manifest.Sources, source => Assert.True(source.LastWriteTimeUtcTicks > 0));
    }

    [Fact]
    public void UpdateWithReuse_WhenSourcesAndParametersAreUnchangedThenSkipsRecompile()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "first.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        var update = new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] };
        LocalDictionaryPackageManager.Update(update, packagePath);
        var markerPath = Path.Combine(packagePath, "reuse.marker");
        File.WriteAllText(markerPath, "keep");

        var result = LocalDictionaryPackageManager.UpdateWithReuse(update, packagePath);

        Assert.True(result.ReusedExistingPackage);
        Assert.True(File.Exists(markerPath));
        Assert.False(Directory.Exists($"{packagePath}.previous"));
        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void UpdateWithReuse_WhenSourceLastWriteTimeChangesThenRecompiles()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "first.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        var update = new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] };
        LocalDictionaryPackageManager.Update(update, packagePath);
        File.SetLastWriteTimeUtc(sourcePath, File.GetLastWriteTimeUtc(sourcePath).AddSeconds(2));

        var result = LocalDictionaryPackageManager.UpdateWithReuse(update, packagePath);

        Assert.False(result.ReusedExistingPackage);
        Assert.True(Directory.Exists($"{packagePath}.previous"));
        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void UpdateWithReuse_WhenInputSchemeChangesThenRecompiles()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "first.phonetic.tsv", "你好\tni hao\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        var result = LocalDictionaryPackageManager.UpdateWithReuse(
            new LocalDictionaryPackageUpdate
            {
                PhoneticSourcePaths = [sourcePath],
                Parameters = new DictionaryPackageParameters { InputScheme = "xiaoheDoublePinyin" },
            },
            packagePath);

        Assert.False(result.ReusedExistingPackage);
        Assert.Equal("你好", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("nihc"))).Text);
    }

    [Fact]
    public void Update_WhenPackageAlreadyExistsThenRetainsPreviousVersion()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "好\thao\t200");

        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load($"{packagePath}.previous").Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Rollback_WhenPreviousVersionExistsThenExchangesVersions()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "好\thao\t200");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);

        LocalDictionaryPackageManager.Rollback(packagePath);

        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Update_WhenSourceIsInvalidThenKeepsActivePackage()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "invalid");

        Assert.Throws<PhoneticDictionarySourceException>(() => LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath));
        Assert.Equal("你", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("ni"))).Text);
    }

    [Fact]
    public void Rollback_WhenPreviousVersionIsInvalidThenKeepsActivePackage()
    {
        var root = CreateRoot();
        var sourcePath = WritePhoneticSource(root, "dictionary.phonetic.tsv", "你\tni\t100");
        var packagePath = Path.Combine(root, "package");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.WriteAllText(sourcePath, "好\thao\t200");
        LocalDictionaryPackageManager.Update(
            new LocalDictionaryPackageUpdate { PhoneticSourcePaths = [sourcePath] },
            packagePath);
        File.Delete(Path.Combine($"{packagePath}.previous", "candidates.bin"));

        Assert.Throws<DictionaryPackageException>(() => LocalDictionaryPackageManager.Rollback(packagePath));
        Assert.Equal("好", Assert.Single(DictionaryPackageLoader.Load(packagePath).Query(new ImeDictionaryQuery("hao"))).Text);
    }

    private static string CreateRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "XiaoXiIme.Dictionary.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string WritePhoneticSource(string root, string fileName, string content)
    {
        var path = Path.Combine(root, fileName);
        File.WriteAllText(path, content);
        return path;
    }
}
