using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LightTextEditorPlus.Document.UnitTests;
/// <summary>
/// Tests for <see cref = "RunPropertyExtension"/> class.
/// </summary>
[TestClass]
public partial class RunPropertyExtensionTests
{
    /// <summary>
    /// Tests that AsSkiaRunProperty successfully casts when the input is a valid SkiaTextRunProperty instance.
    /// </summary>
    [TestMethod]
    public void AsSkiaRunProperty_ValidSkiaTextRunProperty_ReturnsSameInstance()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        IReadOnlyRunProperty runProperty = textEditor.TextEditorCore.DocumentManager.StyleRunProperty.AsSkiaRunProperty();
        // Act
        var result = runProperty.AsSkiaRunProperty();
        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(runProperty, result);
    }

    /// <summary>
    /// Tests that AsSkiaRunProperty throws InvalidCastException when the input is not a SkiaTextRunProperty.
    /// </summary>
    [TestMethod]
    public void AsSkiaRunProperty_DifferentImplementation_ThrowsInvalidCastException()
    {
        // Arrange
        var mockRunProperty = new Mock<IReadOnlyRunProperty>();
        IReadOnlyRunProperty runProperty = mockRunProperty.Object;
        // Act & Assert
        Assert.ThrowsExactly<InvalidCastException>(() => runProperty.AsSkiaRunProperty());
    }

    /// <summary>
    /// Tests that AsSkiaRunProperty returns null when the input is null.
    /// Casting null to a reference type succeeds and returns null.
    /// </summary>
    [TestMethod]
    public void AsSkiaRunProperty_NullInput_ReturnsNull()
    {
        // Arrange
        IReadOnlyRunProperty? runProperty = null;
        // Act
        var result = runProperty!.AsSkiaRunProperty();
        // Assert
        Assert.IsNull(result);
    }
}