using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Maintains one optimistic-concurrency-controlled executable design-system draft.
/// </summary>
public sealed class CoursewareDesignSystemDraftBuilder
{
    private readonly object _syncRoot = new();
    private readonly string[] _inputSlideIds;
    private readonly string[] _knownResourceIds;
    private CoursewareDesignSystem _draft = new();
    private bool _isBegun;
    private bool _isFrozen;

    /// <summary>
    /// Initializes a design-system draft for one immutable courseware input.
    /// </summary>
    public CoursewareDesignSystemDraftBuilder(
        IEnumerable<string> inputSlideIds,
        IEnumerable<string> knownResourceIds,
        bool visualAnalysisExecuted)
    {
        ArgumentNullException.ThrowIfNull(inputSlideIds);
        ArgumentNullException.ThrowIfNull(knownResourceIds);
        _inputSlideIds = inputSlideIds.Distinct(StringComparer.Ordinal).ToArray();
        _knownResourceIds = knownResourceIds.Distinct(StringComparer.Ordinal).ToArray();
        VisualAnalysisExecuted = visualAnalysisExecuted;
        DraftId = $"design-draft-{Guid.NewGuid():N}";
    }

    /// <summary>Gets the stable draft identifier.</summary>
    public string DraftId { get; }

    /// <summary>Gets the current revision.</summary>
    public int Revision { get; private set; }

    /// <summary>Gets whether actual image-backed visual analysis was executed.</summary>
    public bool VisualAnalysisExecuted { get; }

    /// <summary>Begins the draft and sets its design intent.</summary>
    public CoursewareDesignDraftMutationResult Begin(string designSystemId, CoursewareDesignIntent designIntent)
    {
        ArgumentNullException.ThrowIfNull(designIntent);
        lock (_syncRoot)
        {
            EnsureMutable();
            if (_isBegun)
            {
                return Failure("DraftAlreadyBegun", "begin", "设计系统草稿已经开始。", ["set_canvas_and_grid"]);
            }

            _draft = new CoursewareDesignSystem
            {
                DesignSystemId = designSystemId,
                DesignIntent = designIntent,
            };
            _isBegun = true;
            return Success(["set_canvas_and_grid"]);
        }
    }

    /// <summary>Replaces canvas and grid sections.</summary>
    public CoursewareDesignDraftMutationResult SetCanvasAndGrid(
        string draftId,
        int revision,
        IReadOnlyList<CoursewareCanvasDesignProfile> canvases,
        CoursewareGridSystem grid)
    {
        ArgumentNullException.ThrowIfNull(canvases);
        ArgumentNullException.ThrowIfNull(grid);
        return Mutate(draftId, revision, designSystem => designSystem with { CanvasProfiles = canvases, Grid = grid }, ["set_typography_tokens"]);
    }

    /// <summary>Replaces typography tokens.</summary>
    public CoursewareDesignDraftMutationResult SetTypography(
        string draftId,
        int revision,
        CoursewareTypographySystem typography)
    {
        ArgumentNullException.ThrowIfNull(typography);
        return Mutate(draftId, revision, designSystem => designSystem with { Typography = typography }, ["set_color_tokens"]);
    }

    /// <summary>Replaces color tokens.</summary>
    public CoursewareDesignDraftMutationResult SetColors(
        string draftId,
        int revision,
        CoursewareColorSystem colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        return Mutate(draftId, revision, designSystem => designSystem with { Colors = colors }, ["set_spacing_and_effect_tokens"]);
    }

    /// <summary>Replaces spacing and effect tokens.</summary>
    public CoursewareDesignDraftMutationResult SetSpacingAndEffects(
        string draftId,
        int revision,
        CoursewareSpacingScale spacing,
        CoursewareEffectSystem effects)
    {
        ArgumentNullException.ThrowIfNull(spacing);
        ArgumentNullException.ThrowIfNull(effects);
        return Mutate(draftId, revision, designSystem => designSystem with { Spacing = spacing, Effects = effects }, ["upsert_design_component"]);
    }

    /// <summary>Idempotently inserts or replaces one component.</summary>
    public CoursewareDesignDraftMutationResult UpsertComponent(
        string draftId,
        int revision,
        CoursewareComponentSpecification component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return Mutate(
            draftId,
            revision,
            designSystem => designSystem with
            {
                Components = Upsert(designSystem.Components, component, item => item.ComponentId),
            },
            ["upsert_design_component", "set_asset_policy"]);
    }

    /// <summary>Replaces the asset policy.</summary>
    public CoursewareDesignDraftMutationResult SetAssetPolicy(
        string draftId,
        int revision,
        CoursewareAssetUsagePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Mutate(draftId, revision, designSystem => designSystem with { AssetPolicy = policy }, ["upsert_page_type"]);
    }

    /// <summary>Idempotently inserts or replaces one page type.</summary>
    public CoursewareDesignDraftMutationResult UpsertPageType(
        string draftId,
        int revision,
        CoursewarePageTypeContract pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        return Mutate(
            draftId,
            revision,
            designSystem => designSystem with
            {
                PageTypes = Upsert(designSystem.PageTypes, pageType, item => item.PageTypeId),
            },
            ["upsert_page_type", "assign_slides_to_page_type"]);
    }

    /// <summary>Replaces assignments for a supplied group of slides.</summary>
    public CoursewareDesignDraftMutationResult AssignSlides(
        string draftId,
        int revision,
        string pageTypeId,
        IReadOnlyList<string> slideIds,
        double? confidence,
        string? rationale)
    {
        ArgumentNullException.ThrowIfNull(slideIds);
        return Mutate(
            draftId,
            revision,
            designSystem =>
            {
                var replacements = slideIds.Select(slideId => new CoursewarePageTypeAssignment
                {
                    SlideId = slideId,
                    PageTypeId = pageTypeId,
                    Confidence = confidence,
                    Rationale = rationale,
                }).ToArray();
                var replacementIds = replacements.Select(item => item.SlideId).ToHashSet(StringComparer.Ordinal);
                return designSystem with
                {
                    PageTypeAssignments = designSystem.PageTypeAssignments
                        .Where(item => !replacementIds.Contains(item.SlideId))
                        .Concat(replacements)
                        .OrderBy(item => item.SlideId, StringComparer.Ordinal)
                        .ToArray(),
                };
            },
            ["assign_slides_to_page_type", "upsert_page_template"]);
    }

    /// <summary>Idempotently inserts or replaces one page template.</summary>
    public CoursewareDesignDraftMutationResult UpsertTemplate(
        string draftId,
        int revision,
        CoursewarePageTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Mutate(
            draftId,
            revision,
            designSystem => designSystem with
            {
                PageTemplates = Upsert(designSystem.PageTemplates, template, item => item.TemplateId),
            },
            ["upsert_page_template", "set_accessibility_and_consistency_rules"]);
    }

    /// <summary>Replaces accessibility and consistency rules.</summary>
    public CoursewareDesignDraftMutationResult SetRules(
        string draftId,
        int revision,
        CoursewareAccessibilityRules accessibility,
        CoursewareConsistencyRules consistency)
    {
        ArgumentNullException.ThrowIfNull(accessibility);
        ArgumentNullException.ThrowIfNull(consistency);
        return Mutate(
            draftId,
            revision,
            designSystem => designSystem with { Accessibility = accessibility, Consistency = consistency },
            ["set_design_evidence_and_assumptions"]);
    }

    /// <summary>Replaces evidence and assumptions.</summary>
    public CoursewareDesignDraftMutationResult SetEvidence(
        string draftId,
        int revision,
        IReadOnlyList<CoursewareDesignDecisionEvidence> evidence,
        IReadOnlyList<CoursewareDesignAssumption> assumptions)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(assumptions);
        return Mutate(
            draftId,
            revision,
            designSystem => designSystem with { Evidence = evidence, Assumptions = assumptions },
            ["complete_courseware_design_system"]);
    }

    /// <summary>Validates and freezes the current draft.</summary>
    public CoursewareDesignDraftCompletionResult Complete(
        string draftId,
        int revision,
        CoursewareDesignSystemValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        lock (_syncRoot)
        {
            var concurrencyFailure = ValidateMutation(draftId, revision);
            if (concurrencyFailure is not null)
            {
                return new CoursewareDesignDraftCompletionResult
                {
                    Success = false,
                    DraftId = DraftId,
                    Revision = Revision,
                    Validation = new CoursewareDesignSystemValidationReport { Diagnostics = concurrencyFailure.Diagnostics },
                };
            }

            var validation = validator.Validate(_draft, _inputSlideIds, _knownResourceIds, VisualAnalysisExecuted);
            if (!validation.IsValid)
            {
                return new CoursewareDesignDraftCompletionResult
                {
                    Success = false,
                    DraftId = DraftId,
                    Revision = Revision,
                    Validation = validation,
                };
            }

            _isFrozen = true;
            Revision++;
            return new CoursewareDesignDraftCompletionResult
            {
                Success = true,
                DraftId = DraftId,
                Revision = Revision,
                DesignSystem = _draft,
                Validation = validation,
            };
        }
    }

    /// <summary>Gets the current immutable draft snapshot.</summary>
    public CoursewareDesignSystem GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _draft;
        }
    }

    private CoursewareDesignDraftMutationResult Mutate(
        string draftId,
        int revision,
        Func<CoursewareDesignSystem, CoursewareDesignSystem> mutation,
        IReadOnlyList<string> nextTools)
    {
        lock (_syncRoot)
        {
            var failure = ValidateMutation(draftId, revision);
            if (failure is not null)
            {
                return failure;
            }

            _draft = mutation(_draft);
            return Success(nextTools);
        }
    }

    private CoursewareDesignDraftMutationResult? ValidateMutation(string draftId, int revision)
    {
        if (!_isBegun)
        {
            return Failure("DraftNotBegun", "$", "必须先调用 begin_courseware_design_system。", ["begin_courseware_design_system"]);
        }

        if (_isFrozen)
        {
            return Failure("DraftFrozen", "$", "设计系统草稿已冻结，不能继续修改。", []);
        }

        if (!string.Equals(draftId, DraftId, StringComparison.Ordinal))
        {
            return Failure("DraftIdMismatch", "draftId", "DraftId 不匹配。", []);
        }

        if (revision != Revision)
        {
            return Failure("RevisionConflict", "revision", $"Revision 冲突，当前值为 {Revision}。", []);
        }

        return null;
    }

    private void EnsureMutable()
    {
        if (_isFrozen)
        {
            throw new InvalidOperationException("设计系统草稿已冻结。");
        }
    }

    private CoursewareDesignDraftMutationResult Success(IReadOnlyList<string> nextTools)
    {
        Revision++;
        return new CoursewareDesignDraftMutationResult
        {
            Success = true,
            DraftId = DraftId,
            Revision = Revision,
            AllowedNextTools = nextTools,
        };
    }

    private CoursewareDesignDraftMutationResult Failure(
        string code,
        string path,
        string message,
        IReadOnlyList<string> nextTools)
    {
        return new CoursewareDesignDraftMutationResult
        {
            Success = false,
            DraftId = DraftId,
            Revision = Revision,
            Diagnostics =
            [
                new CoursewareValidationDiagnostic
                {
                    Code = code,
                    Path = path,
                    Message = message,
                    Severity = CoursewareValidationSeverity.Error,
                },
            ],
            AllowedNextTools = nextTools,
        };
    }

    private static IReadOnlyList<T> Upsert<T>(IReadOnlyList<T> items, T replacement, Func<T, string> keySelector)
    {
        var replacementKey = keySelector(replacement);
        return items
            .Where(item => !string.Equals(keySelector(item), replacementKey, StringComparison.Ordinal))
            .Append(replacement)
            .OrderBy(keySelector, StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>
/// Represents one design-draft mutation result.
/// </summary>
public sealed record CoursewareDesignDraftMutationResult
{
    /// <summary>Gets whether the mutation was committed.</summary>
    public bool Success { get; set; }

    /// <summary>Gets the stable draft identifier.</summary>
    public string DraftId { get; set; } = string.Empty;

    /// <summary>Gets the current revision after the operation.</summary>
    public int Revision { get; set; }

    /// <summary>Gets field-level diagnostics.</summary>
    public IReadOnlyList<CoursewareValidationDiagnostic> Diagnostics { get; set; } = [];

    /// <summary>Gets tools allowed after this operation.</summary>
    public IReadOnlyList<string> AllowedNextTools { get; set; } = [];
}

/// <summary>
/// Represents design-draft completion and freeze results.
/// </summary>
public sealed record CoursewareDesignDraftCompletionResult
{
    /// <summary>Gets whether the draft was frozen.</summary>
    public bool Success { get; set; }

    /// <summary>Gets the stable draft identifier.</summary>
    public string DraftId { get; set; } = string.Empty;

    /// <summary>Gets the current revision.</summary>
    public int Revision { get; set; }

    /// <summary>Gets the frozen design system when successful.</summary>
    public CoursewareDesignSystem? DesignSystem { get; set; }

    /// <summary>Gets the complete deterministic validation report.</summary>
    public CoursewareDesignSystemValidationReport Validation { get; set; } = new();
}
