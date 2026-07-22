using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Creates configured <see cref="SlideChatManager" /> instances for courseware pages.
/// </summary>
public interface ISlideChatManagerFactory
{
    /// <summary>
    /// Creates a configured <see cref="SlideChatManager" /> for one courseware page.
    /// </summary>
    /// <param name="options">The page runtime options. When omitted, the default document context is used.</param>
    /// <param name="cancellationToken">The token used to cancel initialization.</param>
    /// <returns>The configured <see cref="SlideChatManager" />.</returns>
    Task<SlideChatManager> CreateAsync(
        SlideChatManagerFactoryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an independent local fallback manager when configured chat initialization fails.
    /// </summary>
    /// <param name="options">The page runtime options. When omitted, the default document context is used.</param>
    /// <returns>An independent manager with local rendering support.</returns>
    SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null);
}
