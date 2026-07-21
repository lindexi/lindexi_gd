using CoursewarePptxGenerator.Core.Models;

namespace CoursewarePptxGenerator.Core.Analysis;

/// <summary>
/// Builds complete and privacy-safe model input from immutable courseware facts.
/// </summary>
public interface ICoursewareAnalysisInputBuilder
{
    /// <summary>
    /// Builds model input from a loaded local package.
    /// </summary>
    CoursewareAnalysisInput Build(
        CoursewareInputPackage inputPackage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds model input from an immutable source snapshot.
    /// </summary>
    CoursewareAnalysisInput Build(
        CoursewareAnalysisSourceSnapshot sourceSnapshot,
        CancellationToken cancellationToken = default);
}
