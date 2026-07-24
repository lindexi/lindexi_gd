namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Identifies why an image is attached to a page chat request.
/// </summary>
public enum CoursewareChatImageAttachmentKind
{
    /// <summary>
    /// The original screenshot exported for the source page.
    /// </summary>
    SourceScreenshot,

    /// <summary>
    /// An image explicitly selected by the user.
    /// </summary>
    UserSelectedImage,
}
