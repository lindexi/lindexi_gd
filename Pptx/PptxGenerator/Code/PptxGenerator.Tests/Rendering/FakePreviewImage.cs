using PptxGenerator.Models;

namespace PptxGenerator.Tests.Rendering;

/// <summary>
/// 测试用预览图片 Fake 实现，Save 方法为空操作。
/// </summary>
public sealed class FakePreviewImage : IPreviewImage
{
    /// <inheritdoc />
    public void Save(string filePath)
    {
    }

    /// <inheritdoc />
    public void Save(Stream stream)
    {
    }
}
