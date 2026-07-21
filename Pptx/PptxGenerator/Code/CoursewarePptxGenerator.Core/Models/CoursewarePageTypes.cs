namespace CoursewarePptxGenerator.Core.Models;

/// <summary>
/// Defines the supported content-density range for a page type or template.
/// </summary>
public sealed record CoursewareContentDensityRange
{
    /// <summary>Gets the minimum normalized density from zero to one.</summary>
    public double Minimum { get; set; }

    /// <summary>Gets the maximum normalized density from zero to one.</summary>
    public double Maximum { get; set; }
}

/// <summary>
/// Defines one typed content or asset slot.
/// </summary>
public sealed record CoursewareTemplateSlot
{
    /// <summary>Gets the stable slot identifier.</summary>
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Gets the slot kind.</summary>
    public string SlotKind { get; set; } = string.Empty;

    /// <summary>Gets whether the slot is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets the maximum item count, when constrained.</summary>
    public int? MaximumItemCount { get; set; }

    /// <summary>Gets the slot purpose.</summary>
    public string Purpose { get; set; } = string.Empty;
}

/// <summary>
/// Defines one non-negotiable template or page-type constraint.
/// </summary>
public sealed record CoursewareTemplateConstraint
{
    /// <summary>Gets the stable constraint identifier.</summary>
    public string ConstraintId { get; set; } = string.Empty;

    /// <summary>Gets the constraint expression.</summary>
    public string Rule { get; set; } = string.Empty;
}

/// <summary>
/// Defines one bounded adjustment allowed during page generation.
/// </summary>
public sealed record CoursewareTemplateAdjustment
{
    /// <summary>Gets the stable adjustment identifier.</summary>
    public string AdjustmentId { get; set; } = string.Empty;

    /// <summary>Gets the adjustment description.</summary>
    public string Rule { get; set; } = string.Empty;
}

/// <summary>
/// Defines an executable contract for one dynamic page type.
/// </summary>
public sealed record CoursewarePageTypeContract
{
    /// <summary>Gets the stable page-type identifier.</summary>
    public string PageTypeId { get; set; } = string.Empty;

    /// <summary>Gets the user-visible page-type name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the page-type purpose.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets evidence showing that the page type is needed.</summary>
    public IReadOnlyList<CoursewareEvidenceReference> EvidenceReferences { get; set; } = [];

    /// <summary>Gets the supported content-density range.</summary>
    public CoursewareContentDensityRange DensityRange { get; set; } = new();

    /// <summary>Gets content and asset slots.</summary>
    public IReadOnlyList<CoursewareTemplateSlot> Slots { get; set; } = [];

    /// <summary>Gets reusable component identifiers.</summary>
    public IReadOnlyList<string> ComponentIds { get; set; } = [];

    /// <summary>Gets non-negotiable constraints.</summary>
    public IReadOnlyList<CoursewareTemplateConstraint> HardConstraints { get; set; } = [];

    /// <summary>Gets bounded adjustments.</summary>
    public IReadOnlyList<CoursewareTemplateAdjustment> AllowedAdjustments { get; set; } = [];

    /// <summary>Gets the required behavior when content exceeds capacity.</summary>
    public string OverflowStrategy { get; set; } = string.Empty;

    /// <summary>Gets an optional fallback page type.</summary>
    public string? FallbackPageTypeId { get; set; }
}

/// <summary>
/// Maps one input slide to its primary page type.
/// </summary>
public sealed record CoursewarePageTypeAssignment
{
    /// <summary>Gets the input slide identifier.</summary>
    public string SlideId { get; set; } = string.Empty;

    /// <summary>Gets the selected primary page-type identifier.</summary>
    public string PageTypeId { get; set; } = string.Empty;

    /// <summary>Gets the assignment confidence from zero to one.</summary>
    public double? Confidence { get; set; }

    /// <summary>Gets whether the slide is an outlier within the page type.</summary>
    public bool IsOutlier { get; set; }

    /// <summary>Gets the assignment rationale.</summary>
    public string? Rationale { get; set; }
}

/// <summary>
/// Defines one compiled SlideML page template.
/// </summary>
public sealed record CoursewarePageTemplate
{
    /// <summary>Gets the stable template identifier.</summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>Gets the page type implemented by the template.</summary>
    public string PageTypeId { get; set; } = string.Empty;

    /// <summary>Gets the template name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the template purpose.</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>Gets the supported content-density range.</summary>
    public CoursewareContentDensityRange DensityRange { get; set; } = new();

    /// <summary>Gets the template slots.</summary>
    public IReadOnlyList<CoursewareTemplateSlot> Slots { get; set; } = [];

    /// <summary>Gets the compiled SlideML template.</summary>
    public string SlideMlTemplate { get; set; } = string.Empty;

    /// <summary>Gets non-negotiable constraints.</summary>
    public IReadOnlyList<CoursewareTemplateConstraint> HardConstraints { get; set; } = [];

    /// <summary>Gets bounded adjustments.</summary>
    public IReadOnlyList<CoursewareTemplateAdjustment> AllowedAdjustments { get; set; } = [];

    /// <summary>Gets stress-sample identifiers used during validation.</summary>
    public IReadOnlyList<string> StressSampleIds { get; set; } = [];

    /// <summary>Gets an optional fallback template.</summary>
    public string? FallbackTemplateId { get; set; }
}
