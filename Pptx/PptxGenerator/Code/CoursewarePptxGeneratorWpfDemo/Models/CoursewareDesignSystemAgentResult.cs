using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the validated executable design-system result produced by the analysis agent.
/// </summary>
public sealed record CoursewareDesignSystemAgentResult
{
    /// <summary>Gets the frozen executable design system.</summary>
    public required CoursewareDesignSystem DesignSystem { get; init; }

    /// <summary>Gets deterministic core validation results.</summary>
    public required CoursewareDesignSystemValidationReport Validation { get; init; }

    /// <summary>Gets the image-backed visual-analysis report.</summary>
    public CoursewareVisualAnalysisReport VisualAnalysis { get; init; } = new();
}
