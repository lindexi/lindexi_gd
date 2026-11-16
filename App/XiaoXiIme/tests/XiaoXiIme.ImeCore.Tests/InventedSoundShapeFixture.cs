using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore.Tests;

// 人工数据只验证合同；F/X 的 hk、kr、rh 不是实际小鹤编码。
internal sealed class InventedSoundShapeFixture
{
    internal List<ImeDictionaryLearning> Learning { get; } = [];
    internal ImeContext Context { get; }

    internal InventedSoundShapeFixture
        (string set, ImeInputOptions? options = null, string projection = "F", int aFrequency = 120)
    {
        Context = new ImeContext
            (new InMemoryImeDictionary(Lexicon(set, projection, aFrequency)), options ?? Options(), Learning.Add);
    }

    internal static ImeInputOptions Options
    (
        ImeShapeCasePolicy casePolicy = ImeShapeCasePolicy.UppercaseOrGuess,
        ImeShapePlacement placement = ImeShapePlacement.PerCharacter,
        ImeShapeLengthPolicy length = ImeShapeLengthPolicy.OptionalOneOrTwo,
        ImeCandidateSortPolicy sort = ImeCandidateSortPolicy.PhoneticFirst,
        ImeAutoCommitPolicy auto = ImeAutoCommitPolicy.Off,
        bool eat = false,
        IReadOnlyList<ImeFuzzyRule>? rules = null
    ) => new()
    {
        CasePolicy = casePolicy,
        Placement = placement,
        LengthPolicy = length,
        SortPolicy = sort,
        AutoCommit = auto,
        EatConfirmationSpace = eat,
        FuzzyRules = rules ?? [],
    };

    internal ImeProcessResult Type(string input)
    {
        var result = Context.ProcessKey(ImeKey.FromCharacter(input[0]));
        foreach (var character in input[1..])
        {
            result = Context.ProcessKey(ImeKey.FromCharacter(character));
        }

        return result;
    }

    internal ImeConversionCandidate Find(string text, int end, ImeInterpretationKind kind) =>
        Context.QueryInterpretations(Context.Snapshot.Composition.Reading).Candidates.Single
        (candidate =>
            candidate.Candidate.Text == text && candidate.Interpretations.Any
                (path => path.Kind == kind && path.CoverageEnd == end)
        );

    internal ImeProcessResult Pick(string text, int end, ImeInterpretationKind kind = ImeInterpretationKind.Complete) =>
        Context.SelectCandidate(Find(text, end, kind).SelectionId!.Value);

    internal string[] Complete() => Context.QueryInterpretations(Context.Snapshot.Composition.Reading).Candidates
        .Where(candidate => candidate.Interpretations.Any(path => path.Kind == ImeInterpretationKind.Complete))
        .Select(candidate => candidate.Candidate.Text).ToArray();

    internal static ImeLexicon Lexicon(string set, string projection = "F", int aFrequency = 120)
    {
        var entries = new System.Collections.Generic.Dictionary<string, ImePhoneticEntry>
        {
            ["N"] = new("你", "ni", 40), ["D"] = new("泥", "ni", 500), ["I"] = new("妮", "ni", 300),
            ["A"] = new("你好", "ni hao", aFrequency), ["B"] = new("泥好", "ni hao", 900), ["C"] = new("你号", "ni hao", 700),
            ["R"] = new("你让草", "ni rang cao", 250), ["M"] = new("吗", "ma", 60), ["W"] = new("我", "wo", 70),
            ["O"] = new("你好哪", "ni hao na", 600), ["V"] = new("你好啊", "ni hao a", 1500),
            ["T"] = new("你好我", "ni hao wo", 100), ["P"] = new("拟", "ni", 80),
        };
        var sets = new System.Collections.Generic.Dictionary<string, string[]>
        {
            ["ABC"] = ["A", "B", "C"], ["AB"] = ["A", "B"], ["N3"] = ["N", "D", "I"],
            ["PATH"] = ["A", "R"], ["TAIL"] = ["A", "M"], ["XBOUND"] = ["A"],
            ["CONFLICT"] = ["A", "O"], ["AV"] = ["A", "V"], ["NA"] = ["N", "A"], ["NP"] = ["N", "P"],
            ["N"] = ["N"], ["A"] = ["A"], ["T"] = ["T"],
        };
        var shapes = new (string Text, string Code)[]
        {
            ("你", "rx"), ("你", "re"), ("泥", "dx"), ("妮", "nx"), ("好", "nz"), ("好", "rz"),
            ("号", "kz"), ("夯", "lr"), ("让", "yr"), ("草", "cc"), ("吗", "km"), ("我", "wt"), ("哪", "nk"), ("拟", "rx"),
        };
        var definition = projection switch
        {
            "Full" => ImeProjectionDefinition.FullPinyin,
            "Xiaohe" => ImeProjectionDefinition.XiaoheDoublePinyin,
            _ => new ImeProjectionDefinition
            (
                projection,
                [
                    new("n", "i", "ni"), new("h", "ao", projection == "X" ? "hk" : "hc"),
                    new("h", "ang", "kr"), new("r", "ang", "rh"), new("c", "ao", "cc"),
                    new("m", "a", "ma"), new("w", "o", "wo"), new("n", "a", "nz"),
                ]
            ),
        };
        return new ImeLexicon
        (
            sets[set].Select(id => entries[id]).ToArray(),
            shapes.Where(shape => shape.Text != "拟" || set == "NP")
                .Select(shape => new ShapeDictionaryEntry(shape.Text, shape.Code, "人工夹具")).ToArray(), definition, 1
        );
    }

    internal static ImeLexicon Z(string projection = "Full") => new
    (
        [new("张", "zhang", 80), new("脏", "zang", 70), new("毡", "zhan", 60), new("簪", "zan", 50)],
        [new("张", "zc", "人工夹具"), new("脏", "od", "人工夹具"), new("毡", "mz", "人工夹具"), new("簪", "vv", "人工夹具")],
        projection == "Full" ? ImeProjectionDefinition.FullPinyin : ImeProjectionDefinition.XiaoheDoublePinyin, 1
    );

    internal static ImeLexicon TS(string projection)
    {
        string[] first = ["转", "钻", "庄", "纂"];
        string[] readings = ["zhuan", "zuan", "zhuang", "zuan"];
        string[] last = ["身", "森", "升", "僧"];
        string[] endings = ["shen", "sen", "sheng", "seng"];
        return new ImeLexicon
        (
            Enumerable.Range(0, 4).SelectMany
            (i => Enumerable.Range(0, 4)
                .Select(j => new ImePhoneticEntry(first[i] + last[j], readings[i] + " " + endings[j], 200 - 4 * i - j))
            ).ToArray(), [],
            projection == "Full" ? ImeProjectionDefinition.FullPinyin : ImeProjectionDefinition.XiaoheDoublePinyin, 1
        );
    }
}