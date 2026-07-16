using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Builds bounded and privacy-safe model input from a loaded courseware package.
/// </summary>
public interface ICoursewareAnalysisInputBuilder
{
    /// <summary>
    /// Builds the analysis input.
    /// </summary>
    /// <param name="inputPackage">The loaded courseware package.</param>
    /// <param name="cancellationToken">The token used to cancel input construction.</param>
    /// <returns>The bounded analysis input.</returns>
    CoursewareAnalysisInput Build(CoursewareInputPackage inputPackage, CancellationToken cancellationToken = default);
}