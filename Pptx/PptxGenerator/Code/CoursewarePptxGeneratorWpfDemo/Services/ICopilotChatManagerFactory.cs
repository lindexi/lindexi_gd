using AgentLib;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Creates independently configured Copilot chat managers for application workloads.
/// </summary>
public interface ICopilotChatManagerFactory
{
    /// <summary>
    /// Creates a chat manager configured for the specified workload.
    /// </summary>
    /// <param name="workload">The requested workload.</param>
    /// <param name="cancellationToken">The token used to cancel initialization.</param>
    /// <returns>The configured chat manager.</returns>
    Task<CopilotChatManager> CreateAsync(
        AgentWorkload workload,
        CancellationToken cancellationToken = default);
}