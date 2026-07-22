using System.ComponentModel;
using CoursewarePptxGenerator.Core.Analysis;
using CoursewarePptxGenerator.Core.Models;
using Microsoft.Extensions.AI;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Exposes optimistic-concurrency-controlled design-system draft tools to the analysis model.
/// </summary>
public sealed class CoursewareDesignSystemToolSet
{
    private readonly CoursewareDesignSystemDraftBuilder _draft;
    private readonly CoursewareDesignSystemValidator _validator;
    private readonly IReadOnlyDictionary<string, CoursewareVisualSample> _visualSamples;

    /// <summary>
    /// Initializes a design-system tool set for one analysis run.
    /// </summary>
    public CoursewareDesignSystemToolSet(
        CoursewareDesignSystemDraftBuilder draft,
        CoursewareDesignSystemValidator validator,
        IReadOnlyList<CoursewareVisualSample> visualSamples)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(visualSamples);
        _draft = draft;
        _validator = validator;
        _visualSamples = visualSamples.ToDictionary(sample => sample.SlideId, StringComparer.Ordinal);
    }

    /// <summary>Gets the frozen completion result.</summary>
    public CoursewareDesignDraftCompletionResult? Completion { get; private set; }

    /// <summary>Gets validated visual observations submitted by the model.</summary>
    public IReadOnlyList<CoursewareVisualObservation> VisualObservations { get; private set; } = [];

    /// <summary>Creates all design-system draft tools.</summary>
    public IReadOnlyList<AIFunction> CreateTools()
    {
        return
        [
            AIFunctionFactory.Create(Begin, "begin_courseware_design_system", "创建设计系统草稿并提交设计意图。"),
            AIFunctionFactory.Create(SetCanvasAndGrid, "set_canvas_and_grid", "提交主画布、其他画布和网格安全区。"),
            AIFunctionFactory.Create(SetTypography, "set_typography_tokens", "替换完整字体与文本层级令牌。"),
            AIFunctionFactory.Create(SetColors, "set_color_tokens", "替换完整语义颜色令牌。"),
            AIFunctionFactory.Create(SetSpacingAndEffects, "set_spacing_and_effect_tokens", "替换间距和视觉效果令牌。"),
            AIFunctionFactory.Create(UpsertComponent, "upsert_design_component", "按 ComponentId 新增或更新一个组件。"),
            AIFunctionFactory.Create(SetAssetPolicy, "set_asset_policy", "替换逻辑素材使用策略。"),
            AIFunctionFactory.Create(UpsertPageType, "upsert_page_type", "按 PageTypeId 新增或更新一个动态页面类型。"),
            AIFunctionFactory.Create(AssignSlides, "assign_slides_to_page_type", "把一组 SlideId 映射到一个主要页面类型。"),
            AIFunctionFactory.Create(UpsertTemplate, "upsert_page_template", "按 TemplateId 新增或更新一个可编译 SlideML 模板。"),
            AIFunctionFactory.Create(SetRules, "set_accessibility_and_consistency_rules", "替换无障碍和跨页一致性规则。"),
            AIFunctionFactory.Create(SetEvidence, "set_design_evidence_and_assumptions", "替换设计证据、假设和实际图像观察。"),
            AIFunctionFactory.Create(Complete, "complete_courseware_design_system", "执行完整校验并冻结设计系统。"),
        ];
    }

    [Description("创建一个新的课件设计系统草稿。")]
    public CoursewareDesignDraftMutationResult Begin(
        [Description("稳定的设计系统 ID。")]
        string designSystemId,
        [Description("设计意图。")]
        CoursewareDesignIntent designIntent)
    {
        return _draft.Begin(designSystemId, designIntent);
    }

    public CoursewareDesignDraftMutationResult SetCanvasAndGrid(
        string draftId,
        int revision,
        IReadOnlyList<CoursewareCanvasDesignProfile> canvases,
        CoursewareGridSystem grid)
    {
        return _draft.SetCanvasAndGrid(draftId, revision, canvases, grid);
    }

    public CoursewareDesignDraftMutationResult SetTypography(
        string draftId,
        int revision,
        CoursewareTypographySystem typography)
    {
        return _draft.SetTypography(draftId, revision, typography);
    }

    public CoursewareDesignDraftMutationResult SetColors(
        string draftId,
        int revision,
        CoursewareColorSystem colors)
    {
        return _draft.SetColors(draftId, revision, colors);
    }

    public CoursewareDesignDraftMutationResult SetSpacingAndEffects(
        string draftId,
        int revision,
        CoursewareSpacingScale spacing,
        CoursewareEffectSystem effects)
    {
        return _draft.SetSpacingAndEffects(draftId, revision, spacing, effects);
    }

    public CoursewareDesignDraftMutationResult UpsertComponent(
        string draftId,
        int revision,
        CoursewareComponentSpecification component)
    {
        return _draft.UpsertComponent(draftId, revision, component);
    }

    public CoursewareDesignDraftMutationResult SetAssetPolicy(
        string draftId,
        int revision,
        CoursewareAssetUsagePolicy assetPolicy)
    {
        return _draft.SetAssetPolicy(draftId, revision, assetPolicy);
    }

    public CoursewareDesignDraftMutationResult UpsertPageType(
        string draftId,
        int revision,
        CoursewarePageTypeContract pageType)
    {
        return _draft.UpsertPageType(draftId, revision, pageType);
    }

    public CoursewareDesignDraftMutationResult AssignSlides(
        string draftId,
        int revision,
        string pageTypeId,
        IReadOnlyList<string> slideIds,
        double? confidence = null,
        string? rationale = null)
    {
        return _draft.AssignSlides(draftId, revision, pageTypeId, slideIds, confidence, rationale);
    }

    public CoursewareDesignDraftMutationResult UpsertTemplate(
        string draftId,
        int revision,
        CoursewarePageTemplate template)
    {
        return _draft.UpsertTemplate(draftId, revision, template);
    }

    public CoursewareDesignDraftMutationResult SetRules(
        string draftId,
        int revision,
        CoursewareAccessibilityRules accessibility,
        CoursewareConsistencyRules consistency)
    {
        return _draft.SetRules(draftId, revision, accessibility, consistency);
    }

    public CoursewareDesignDraftMutationResult SetEvidence(
        string draftId,
        int revision,
        IReadOnlyList<CoursewareDesignDecisionEvidence> evidence,
        IReadOnlyList<CoursewareDesignAssumption> assumptions,
        IReadOnlyList<CoursewareVisualObservation>? visualObservations = null)
    {
        var observations = visualObservations ?? [];
        var invalidObservations = observations
            .Where(observation => !_draft.VisualAnalysisExecuted
                || !_visualSamples.TryGetValue(observation.SlideId, out var sample)
                || sample.PageNumber != observation.PageNumber
                || sample.Role != observation.SampleRole
                || observation.Confidence is < 0 or > 1)
            .ToArray();
        if (invalidObservations.Length > 0)
        {
            return new CoursewareDesignDraftMutationResult
            {
                Success = false,
                DraftId = _draft.DraftId,
                Revision = _draft.Revision,
                Diagnostics = invalidObservations.Select(observation => new CoursewareValidationDiagnostic
                {
                    Code = "InvalidVisualObservation",
                    Path = $"visualObservations[{observation.ObservationId}]",
                    Message = "视觉观察必须绑定本次实际发送的截图样本、页码和样本角色，且置信度位于 0 到 1。",
                    Severity = CoursewareValidationSeverity.Error,
                }).ToArray(),
                AllowedNextTools = ["set_design_evidence_and_assumptions"],
            };
        }

        var result = _draft.SetEvidence(draftId, revision, evidence, assumptions);
        if (result.Success)
        {
            VisualObservations = observations;
        }

        return result;
    }

    public CoursewareDesignDraftCompletionResult Complete(string draftId, int revision)
    {
        Completion = _draft.Complete(draftId, revision, _validator);
        return Completion;
    }
}
