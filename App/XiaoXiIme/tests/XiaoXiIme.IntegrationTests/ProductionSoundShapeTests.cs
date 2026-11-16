using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeCore;

namespace XiaoXiIme.IntegrationTests;

public class ProductionSoundShapeTests : IClassFixture<RealSoundShapePackageFixture>
{
    private readonly RealSoundShapePackageFixture fixture;

    public ProductionSoundShapeTests(RealSoundShapePackageFixture fixture) => this.fixture = fixture;

    public static IEnumerable<object[]> ShapeCases()
    {
        foreach (var scheme in new[]
                 {
                     DictionaryPackageLocations.FullPinyinInputScheme,
                     DictionaryPackageLocations.XiaoheDoublePinyinInputScheme
                 })
        foreach (var policy in Enum.GetValues<ImeShapeCasePolicy>())
        foreach (var placement in Enum.GetValues<ImeShapePlacement>())
        foreach (var length in Enum.GetValues<ImeShapeLengthPolicy>())
        {
            if (length == ImeShapeLengthPolicy.RequiredTwo && placement != ImeShapePlacement.PerCharacter)
            {
                continue;
            }

            var shape = length == ImeShapeLengthPolicy.OptionalOne ? "r" : "rx";
            if (policy != ImeShapeCasePolicy.LowercaseOnly)
            {
                shape = shape.ToUpperInvariant();
            }

            yield return [scheme, policy, placement, length, "ni" + shape];
        }
    }

    [Theory]
    [MemberData(nameof(ShapeCases))]
    public void RealMoqiShapeRespectsEveryLegalCasePlacementAndLength
    (
        string scheme, ImeShapeCasePolicy policy, ImeShapePlacement placement, ImeShapeLengthPolicy length, string input
    )
    {
        var context = Create(scheme, policy, placement, length);
        var page = context.QueryInterpretations(input);

        Assert.Contains
        (
            page.Candidates, c => c.Candidate.Text == "你" && c.Candidate.Reading == "ni" &&
                                  c.Interpretations.Any
                                  (p => p.Kind == ImeInterpretationKind.Complete &&
                                        p.Evidence.Any
                                        (e => e.Kind is ImeEvidenceKind.PerCharacterShape
                                            or ImeEvidenceKind.AfterWordShape
                                        )
                                  )
        );
    }

    [Theory]
    [InlineData("fullPinyin", "nihao", "你好", "ni hao")]
    [InlineData("xiaoheDoublePinyin", "nihc", "你好", "ni hao")]
    [InlineData("fullPinyin", "niRXhaoNZ", "你好", "ni hao")]
    [InlineData("xiaoheDoublePinyin", "niRXhcNZ", "你好", "ni hao")]
    [InlineData("fullPinyin", "nihaoRN", "你好", "ni hao")]
    [InlineData("xiaoheDoublePinyin", "nihcRN", "你好", "ni hao")]
    public void RealWordHasCompleteCanonicalInterpretation(string scheme, string input, string text, string reading)
    {
        var context = Create
        (
            scheme, ImeShapeCasePolicy.UppercaseOnly, ImeShapePlacement.Both,
            ImeShapeLengthPolicy.OptionalOneOrTwo
        );

        Assert.Contains
        (
            context.QueryInterpretations(input).Candidates, c => c.Candidate.Text == text &&
                                                                 c.Candidate.Reading == reading && c.Interpretations.Any
                                                                     (p => p.Kind == ImeInterpretationKind.Complete)
        );
    }

    [Theory]
    [InlineData("fullPinyin", "niRXhaoNZ", "niRX")]
    [InlineData("xiaoheDoublePinyin", "niRXhcNZ", "niRX")]
    public void SelectingRealPartialCandidateConsumesOnlyItsEvidence(string scheme, string input, string consumed)
    {
        var context = Create
        (
            scheme, ImeShapeCasePolicy.UppercaseOnly, ImeShapePlacement.PerCharacter,
            ImeShapeLengthPolicy.OptionalOneOrTwo
        );
        foreach (var character in input)
        {
            context.ProcessKey(ImeKey.FromCharacter(character));
        }

        var candidate = context.QueryInterpretations(input).Candidates.Single
        (c => c.Candidate.Text == "你" &&
              c.Interpretations.Any(p => p.Kind == ImeInterpretationKind.Partial && p.CoverageEnd == consumed.Length)
        );

        var result = context.SelectCandidate(candidate.SelectionId!.Value);

        Assert.Equal(("你", input[consumed.Length..]), (result.CommitText, context.Snapshot.Composition.Reading));
    }

    [Theory]
    [InlineData("fullPinyin", "niNZ")]
    [InlineData("xiaoheDoublePinyin", "niNZ")]
    public void WrongRealShapeCannotEnterMatchingPhoneticPath(string scheme, string input)
    {
        var context = Create
        (
            scheme, ImeShapeCasePolicy.UppercaseOnly, ImeShapePlacement.PerCharacter,
            ImeShapeLengthPolicy.OptionalOneOrTwo
        );

        Assert.DoesNotContain
        (
            context.QueryInterpretations(input).Candidates, c => c.Candidate.Text == "你" &&
                                                                 c.Interpretations.Any
                                                                     (p => p.Kind == ImeInterpretationKind.Complete)
        );
    }

    [Theory]
    [InlineData("fullPinyin", "zan", "张", "zhang", ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn)]
    [InlineData("xiaoheDoublePinyin", "zj", "张", "zhang", ImeFuzzyRule.ZhZ, ImeFuzzyRule.AngAn)]
    [InlineData
    (
        "fullPinyin", "zuangseng", "转身", "zhuan shen", ImeFuzzyRule.ZhZ, ImeFuzzyRule.UanUang, ImeFuzzyRule.ShS,
        ImeFuzzyRule.EngEn
    )]
    [InlineData
    (
        "xiaoheDoublePinyin", "zlsg", "转身", "zhuan shen", ImeFuzzyRule.ZhZ, ImeFuzzyRule.UanUang, ImeFuzzyRule.ShS,
        ImeFuzzyRule.EngEn
    )]
    [InlineData("fullPinyin", "xiang", "先", "xian", ImeFuzzyRule.IanIang)]
    [InlineData("xiaoheDoublePinyin", "xl", "先", "xian", ImeFuzzyRule.IanIang)]
    [InlineData("fullPinyin", "chong", "春", "chun", ImeFuzzyRule.UnOng)]
    [InlineData("xiaoheDoublePinyin", "is", "春", "chun", ImeFuzzyRule.UnOng)]
    public void DynamicRulesReturnRealCanonicalWordAndDisableImmediately
    (
        string scheme, string input, string text, string reading, params ImeFuzzyRule[] rules
    )
    {
        var context = Create
        (
            scheme, ImeShapeCasePolicy.UppercaseOnly, ImeShapePlacement.Both,
            ImeShapeLengthPolicy.OptionalOneOrTwo
        );
        context.UpdateFuzzyRules(rules);
        Assert.Contains
        (
            context.QueryInterpretations(input).Candidates, c => c.Candidate.Text == text &&
                                                                 c.Candidate.Reading == reading && c.Interpretations.Any
                                                                     (p => p.Kind == ImeInterpretationKind.Complete)
        );

        context.UpdateFuzzyRules([]);
        Assert.DoesNotContain
        (
            context.QueryInterpretations(input).Candidates, c => c.Candidate.Text == text &&
                                                                 c.Interpretations.Any
                                                                     (p => p.Kind == ImeInterpretationKind.Complete)
        );
    }

    private ImeContext Create
    (
        string scheme, ImeShapeCasePolicy policy, ImeShapePlacement placement,
        ImeShapeLengthPolicy length
    )
    {
        var package = DictionaryPackageLocations.ResolvePackageDirectory
            (inputScheme: scheme, baseDirectory: fixture.HostRoot);
        return new
        (
            DictionaryPackageLoader.Load(package), new ImeInputOptions
            {
                CasePolicy = policy, Placement = placement, LengthPolicy = length,
            }, null
        );
    }
}