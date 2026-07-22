using PptxGenerator.Models;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Configures the independent SlideML runtime created for one courseware page.
/// </summary>
public sealed class SlideChatManagerFactoryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlideChatManagerFactoryOptions" /> class.
    /// </summary>
    /// <param name="documentContext">The immutable document context shared by prompting, layout, and rendering.</param>
    public SlideChatManagerFactoryOptions(SlideDocumentContext documentContext)
    {
        ArgumentNullException.ThrowIfNull(documentContext);
        DocumentContext = documentContext;
    }

    /// <summary>
    /// Gets the immutable document context shared by prompting, layout, and rendering.
    /// </summary>
    public SlideDocumentContext DocumentContext { get; }

    /// <summary>
    /// Gets or initializes a value indicating whether the default MCP endpoint should be tried in the background.
    /// </summary>
    public bool TryEnableDefaultMcp { get; init; } = true;
}
