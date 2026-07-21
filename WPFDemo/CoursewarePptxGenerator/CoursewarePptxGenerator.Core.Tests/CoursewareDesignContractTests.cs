using System.Text.Json;
using CoursewarePptxGenerator.Core.Models;
using CoursewarePptxGenerator.Core.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoursewarePptxGenerator.Core.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CoursewareDesignContractTests
{
    [TestMethod(DisplayName = "设计系统契约应使用固定 v2 Schema 并可由源生成上下文往返序列化")]
    [Timeout(60_000)]
    public void DesignSystemContractShouldRoundTripWithVersionedSourceGeneratedMetadata()
    {
        var result = CreateResult();

        var json = JsonSerializer.Serialize(
            result,
            CoursewareDesignJsonSerializerContext.Default.CoursewareDesignAnalysisResult);
        var roundTripped = JsonSerializer.Deserialize(
            json,
            CoursewareDesignJsonSerializerContext.Default.CoursewareDesignAnalysisResult);

        Assert.IsNotNull(roundTripped);
        Assert.AreEqual("2.0", roundTripped.SchemaVersion);
        Assert.AreEqual("2.0", roundTripped.DesignSystem.SchemaVersion);
        Assert.AreEqual("design-system-test", roundTripped.DesignSystem.DesignSystemId);
        Assert.AreEqual(CoursewareCapabilityStatus.Passed, roundTripped.CapabilityStates.DesignSystem);
        Assert.HasCount(1, roundTripped.DesignSystem.PageTypes);
        Assert.HasCount(1, roundTripped.DesignSystem.PageTypeAssignments);
        Assert.HasCount(1, roundTripped.DesignSystem.Evidence);
        Assert.HasCount(1, roundTripped.DesignSystem.Assumptions);
        StringAssert.Contains(json, "\"schemaVersion\": \"2.0\"");
        StringAssert.Contains(json, "\"pageTypeAssignments\"");
    }

    [TestMethod(DisplayName = "视觉能力未执行时不得伪装为已通过")]
    [Timeout(60_000)]
    public void VisualCapabilityShouldRemainIndependentFromDesignSystemSuccess()
    {
        var result = CreateResult();

        Assert.AreEqual(CoursewareCapabilityStatus.Passed, result.CapabilityStates.DesignSystem);
        Assert.AreEqual(CoursewareCapabilityStatus.NotRequested, result.CapabilityStates.VisualAnalysis);
        Assert.AreEqual(CoursewareEvidenceSourceKind.DesignInference, result.DesignSystem.Assumptions[0].Sources[0].SourceKind);
    }

    private static CoursewareDesignAnalysisResult CreateResult()
    {
        return new CoursewareDesignAnalysisResult
        {
            DesignSystem = new CoursewareDesignSystem
            {
                DesignSystemId = "design-system-test",
                DesignIntent = new CoursewareDesignIntent
                {
                    Name = "测试设计系统",
                    Summary = "用于验证 v2 契约。",
                    Rationale = "根据完整 Markdown 结构重新设计。",
                    StyleKeywords = ["清晰", "克制"],
                },
                CanvasProfiles =
                [
                    new CoursewareCanvasDesignProfile
                    {
                        CanvasId = "canvas-primary",
                        Width = 1280,
                        Height = 720,
                        IsPrimary = true,
                    },
                ],
                Grid = new CoursewareGridSystem
                {
                    SafeLeft = 64,
                    SafeTop = 48,
                    SafeRight = 64,
                    SafeBottom = 48,
                    ColumnCount = 12,
                    Gutter = 24,
                    Baseline = 8,
                },
                Spacing = new CoursewareSpacingScale
                {
                    Tokens = [new CoursewareSpacingToken { TokenId = "space-md", Value = 24, Purpose = "组件间距" }],
                },
                Typography = new CoursewareTypographySystem
                {
                    Tokens =
                    [
                        new CoursewareTypographyToken
                        {
                            TokenId = "body-primary",
                            EastAsianFontStack = ["Microsoft YaHei"],
                            LatinFontStack = ["Arial"],
                            FontSize = 32,
                            FontWeight = "Regular",
                            LineHeight = 1.35,
                            Purpose = "正文",
                            AllowedPageTypeIds = ["content"],
                        },
                    ],
                },
                Colors = new CoursewareColorSystem
                {
                    Tokens =
                    [
                        new CoursewareColorToken
                        {
                            TokenId = "text-primary",
                            HexValue = "#0F172A",
                            Purpose = "主要文字",
                            AllowedBackgroundTokenIds = ["background-page"],
                            SourceKind = CoursewareEvidenceSourceKind.DesignInference,
                        },
                        new CoursewareColorToken
                        {
                            TokenId = "background-page",
                            HexValue = "#FFFFFF",
                            Purpose = "页面背景",
                            SourceKind = CoursewareEvidenceSourceKind.DesignInference,
                        },
                    ],
                },
                Effects = new CoursewareEffectSystem(),
                AssetPolicy = new CoursewareAssetUsagePolicy(),
                PageTypes =
                [
                    new CoursewarePageTypeContract
                    {
                        PageTypeId = "content",
                        Name = "内容页",
                        Purpose = "承载单一教学主题",
                        EvidenceReferences =
                        [
                            new CoursewareEvidenceReference
                            {
                                SourceKind = CoursewareEvidenceSourceKind.SlideMarkdownFact,
                                SlideId = "slide-1",
                                PageNumber = 1,
                                Description = "页面包含标题和正文。",
                            },
                        ],
                        DensityRange = new CoursewareContentDensityRange { Minimum = 0.2, Maximum = 0.8 },
                        Slots =
                        [
                            new CoursewareTemplateSlot
                            {
                                SlotId = "title",
                                SlotKind = "Text",
                                IsRequired = true,
                                Purpose = "页面标题",
                            },
                        ],
                        OverflowStrategy = "Split",
                    },
                ],
                PageTypeAssignments =
                [
                    new CoursewarePageTypeAssignment
                    {
                        SlideId = "slide-1",
                        PageTypeId = "content",
                        Confidence = 0.95,
                    },
                ],
                Accessibility = new CoursewareAccessibilityRules
                {
                    MinimumBodyFontSize = 28,
                    MinimumNormalTextContrastRatio = 4.5,
                    MinimumLargeTextContrastRatio = 3,
                },
                Consistency = new CoursewareConsistencyRules
                {
                    Invariants = ["字体家族", "语义颜色"],
                    AllowedVariations = ["页面构图"],
                },
                Evidence =
                [
                    new CoursewareDesignDecisionEvidence
                    {
                        DecisionId = "decision-color",
                        DecisionKind = "ColorSystem",
                        SelectedValue = "高对比中性背景",
                        Confidence = 0.9,
                        Sources =
                        [
                            new CoursewareEvidenceReference
                            {
                                SourceKind = CoursewareEvidenceSourceKind.DeterministicStatistic,
                                StatisticId = "background-color-frequency",
                                Description = "页面背景以浅色为主。",
                            },
                        ],
                    },
                ],
                Assumptions =
                [
                    new CoursewareDesignAssumption
                    {
                        AssumptionId = "assumption-brand",
                        Description = "未确认重复素材是否为品牌标识。",
                        Confidence = 0.2,
                        Sources =
                        [
                            new CoursewareEvidenceReference
                            {
                                SourceKind = CoursewareEvidenceSourceKind.DesignInference,
                                Description = "当前没有视觉证据。",
                            },
                        ],
                    },
                ],
            },
            CapabilityStates = new CoursewareAnalysisCapabilityStates
            {
                TextAnalysis = CoursewareCapabilityStatus.Passed,
                DesignSystem = CoursewareCapabilityStatus.Passed,
                TemplateValidation = CoursewareCapabilityStatus.NotRequested,
                VisualAnalysis = CoursewareCapabilityStatus.NotRequested,
                PageGeneration = CoursewareCapabilityStatus.NotRequested,
            },
            AnalyzedAt = DateTimeOffset.UtcNow,
            TotalSlideCount = 1,
            AnalyzedSlideCount = 1,
            InputFingerprint = "fingerprint-test",
        };
    }
}
