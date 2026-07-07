using PptxGenerator.Models;
using System.IO;

namespace CoursewarePptxGeneratorWpfDemo.Rendering;

/// <summary>
/// Represents an empty preview image used before real rendering is available.
/// </summary>
internal sealed class EmptyPreviewImage : IPreviewImage
{
    /// <inheritdoc />
    public void Save(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        File.WriteAllBytes(filePath, []);
    }

    /// <inheritdoc />
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
    }
}
