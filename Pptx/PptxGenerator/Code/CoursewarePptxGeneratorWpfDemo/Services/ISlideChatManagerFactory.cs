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
    /// <returns>The configured <see cref="SlideChatManager" />.</returns>
    Task<SlideChatManager> CreateAsync();
}
