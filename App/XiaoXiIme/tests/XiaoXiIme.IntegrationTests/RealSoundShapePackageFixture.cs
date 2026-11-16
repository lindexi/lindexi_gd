using XiaoXiIme.Dictionary;

namespace XiaoXiIme.IntegrationTests;

public sealed class RealSoundShapePackageFixture
{
    public string HostRoot { get; } = Path.Combine(Path.GetTempPath(), "XiaoXiIme.RealSoundShape", Guid.NewGuid().ToString("N"));

    public RealSoundShapePackageFixture()
    {
        var sourceRoot = Path.Combine(AppContext.BaseDirectory, "DictionarySources");
        var phoneticPath = Path.Combine(sourceRoot, "phonetic", "sewzc-default.phonetic.tsv");
        var shapePath = Path.Combine(sourceRoot, "shape", "sewzc-moqi.shape.tsv");
        using var phoneticReader = File.OpenText(phoneticPath);
        using var shapeReader = File.OpenText(shapePath);
        var entries = PhoneticDictionarySourceParser.Parse(phoneticReader, phoneticPath);
        var shapes = ShapeDictionarySourceParser.Parse(shapeReader, shapePath);
        (string Text, string Reading, int Frequency)[] expected =
        [
            ("你", "ni", 492791), ("好", "hao", 124546), ("你好", "ni hao", 9999540),
            ("张", "zhang", 15947), ("转身", "zhuan shen", 4255),
            ("先", "xian", 75337), ("香", "xiang", 5918),
            ("春", "chun", 2039), ("冲", "chong", 6622),
        ];
        var selected = expected.Select(e => entries.Single(actual => actual.Text == e.Text && actual.Reading == e.Reading)).ToArray();
        Assert.Equal(expected.Select(e => e.Frequency), selected.Select(e => e.Frequency));
        (string Text, string Code)[] expectedShapes = [("你", "rx"), ("好", "nz"), ("张", "gi"), ("转", "iv"), ("身", "zc")];
        var selectedShapes = expectedShapes.Select(e => shapes.Single(actual => actual.Text == e.Text && actual.ShapeCode == e.Code)).ToArray();
        foreach (var scheme in new[] { DictionaryPackageLocations.FullPinyinInputScheme, DictionaryPackageLocations.XiaoheDoublePinyinInputScheme })
        {
            var projection = scheme == DictionaryPackageLocations.FullPinyinInputScheme
                ? ImeProjectionDefinition.FullPinyin : ImeProjectionDefinition.XiaoheDoublePinyin;
            var lexicon = new ImeLexicon(selected.Select(e => new ImePhoneticEntry(e.Text, e.Reading, e.Frequency)).ToArray(),
                selectedShapes, projection, 1);
            DictionaryPackageCompiler.CompileLexicon(lexicon,
                DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: scheme, baseDirectory: HostRoot));
        }
    }
}
