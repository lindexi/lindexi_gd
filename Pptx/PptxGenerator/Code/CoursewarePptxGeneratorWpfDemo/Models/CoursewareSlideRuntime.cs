using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents the lazily created runtime owned by one courseware slide.
/// </summary>
public sealed class CoursewareSlideRuntime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoursewareSlideRuntime" /> class.
    /// </summary>
    /// <param name="slideChatManager">The independent SlideML chat and render manager.</param>
    /// <param name="isAiGenerationAvailable">Whether language-model generation is available.</param>
    /// <param name="initializationError">The non-blocking initialization error when a local fallback is used.</param>
    public CoursewareSlideRuntime(
        SlideChatManager slideChatManager,
        bool isAiGenerationAvailable,
        string? initializationError = null)
    {
        ArgumentNullException.ThrowIfNull(slideChatManager);
        SlideChatManager = slideChatManager;
        IsAiGenerationAvailable = isAiGenerationAvailable;
        InitializationError = initializationError;
    }

    /// <summary>
    /// Gets the independent SlideML chat and render manager.
    /// </summary>
    public SlideChatManager SlideChatManager { get; }

    /// <summary>
    /// Gets a value indicating whether language-model generation is available.
    /// </summary>
    public bool IsAiGenerationAvailable { get; }

    /// <summary>
    /// Gets the non-blocking initialization error when a local fallback is used.
    /// </summary>
    public string? InitializationError { get; }
}
