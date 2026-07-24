using System.IO;
using CoursewarePptxGeneratorWpfDemo.Models;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Represents one visible image attached to the next page chat request.
/// </summary>
public sealed class CoursewareChatImageAttachmentViewModel
{
    /// <summary>
    /// Initializes a new image attachment.
    /// </summary>
    /// <param name="file">The local image file.</param>
    /// <param name="kind">The attachment origin.</param>
    public CoursewareChatImageAttachmentViewModel(
        FileInfo file,
        CoursewareChatImageAttachmentKind kind)
    {
        ArgumentNullException.ThrowIfNull(file);
        File = file;
        Kind = kind;
    }

    /// <summary>
    /// Gets the local image file.
    /// </summary>
    public FileInfo File { get; }

    /// <summary>
    /// Gets the attachment origin.
    /// </summary>
    public CoursewareChatImageAttachmentKind Kind { get; }

    /// <summary>
    /// Gets the full image path used by image bindings and the request snapshot.
    /// </summary>
    public string FullName => File.FullName;

    /// <summary>
    /// Gets the display file name.
    /// </summary>
    public string DisplayName => File.Name;

    /// <summary>
    /// Gets a value indicating whether the image still exists.
    /// </summary>
    public bool IsAvailable => File.Exists;
}
