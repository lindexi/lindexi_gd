using System;
using System.Reflection;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Platform.UnitTests;


/// <summary>
/// Tests for <see cref="SkiaTextEditorPlatformProvider"/> class.
/// </summary>
[TestClass]
public class SkiaTextEditorPlatformProviderTests
{
    /// <summary>
    /// Helper class to expose protected GetSkiaPlatformResourceManager method for testing.
    /// </summary>
    private class TestableSkiaTextEditorPlatformProvider : SkiaTextEditorPlatformProvider
    {
    }

    /// <summary>
    /// Tests that GetCharInfoMeasurer returns a non-null instance on first call.
    /// </summary>
    [TestMethod]
    public void GetCharInfoMeasurer_FirstCall_ReturnsNonNullInstance()
    {
        // Arrange
        var provider = new SkiaTextEditorPlatformProvider();

        // Act
        var result = provider.GetCharInfoMeasurer();

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that GetCharInfoMeasurer returns an instance of SkiaCharInfoMeasurer.
    /// </summary>
    [TestMethod]
    public void GetCharInfoMeasurer_FirstCall_ReturnsSkiaCharInfoMeasurerInstance()
    {
        // Arrange
        var provider = new SkiaTextEditorPlatformProvider();

        // Act
        var result = provider.GetCharInfoMeasurer();

        // Assert
        Assert.IsInstanceOfType(result, typeof(SkiaCharInfoMeasurer));
    }

    /// <summary>
    /// Tests that GetCharInfoMeasurer returns the same instance on multiple calls, verifying caching behavior.
    /// </summary>
    [TestMethod]
    public void GetCharInfoMeasurer_MultipleCalls_ReturnsSameInstance()
    {
        // Arrange
        var provider = new SkiaTextEditorPlatformProvider();

        // Act
        var firstCall = provider.GetCharInfoMeasurer();
        var secondCall = provider.GetCharInfoMeasurer();
        var thirdCall = provider.GetCharInfoMeasurer();

        // Assert
        Assert.AreSame(firstCall, secondCall, "Second call should return the same instance as first call.");
        Assert.AreSame(secondCall, thirdCall, "Third call should return the same instance as second call.");
        Assert.AreSame(firstCall, thirdCall, "Third call should return the same instance as first call.");
    }

    /// <summary>
    /// Tests that GetCharInfoMeasurer returns an instance that implements ICharInfoMeasurer interface.
    /// </summary>
    [TestMethod]
    public void GetCharInfoMeasurer_FirstCall_ReturnsICharInfoMeasurerImplementation()
    {
        // Arrange
        var provider = new SkiaTextEditorPlatformProvider();

        // Act
        var result = provider.GetCharInfoMeasurer();

        // Assert
        Assert.IsInstanceOfType<ICharInfoMeasurer>(result);
    }

    /// <summary>
    /// Tests that GetPlatformFontNameManager returns a non-null instance implementing IPlatformFontNameManager.
    /// Condition: First call to GetPlatformFontNameManager.
    /// Expected Result: Returns a valid IPlatformFontNameManager instance.
    /// </summary>
    [TestMethod]
    public void GetPlatformFontNameManager_FirstCall_ReturnsNonNullInstance()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var provider = new SkiaTextEditorPlatformProvider
        {
            TextEditor = mockTextEditor.Object
        };

        // Act
        var result = provider.GetPlatformFontNameManager();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<IPlatformFontNameManager>(result);
    }

    /// <summary>
    /// Tests that GetPlatformFontNameManager returns the same cached instance on multiple calls.
    /// Condition: Multiple sequential calls to GetPlatformFontNameManager.
    /// Expected Result: Returns the same instance each time (lazy initialization caching behavior).
    /// </summary>
    [TestMethod]
    public void GetPlatformFontNameManager_MultipleCalls_ReturnsSameCachedInstance()
    {
        // Arrange
        var mockTextEditor = new Mock<SkiaTextEditor>();
        var provider = new SkiaTextEditorPlatformProvider
        {
            TextEditor = mockTextEditor.Object
        };

        // Act
        var firstCall = provider.GetPlatformFontNameManager();
        var secondCall = provider.GetPlatformFontNameManager();
        var thirdCall = provider.GetPlatformFontNameManager();

        // Assert
        Assert.IsNotNull(firstCall);
        Assert.AreSame(firstCall, secondCall);
        Assert.AreSame(firstCall, thirdCall);
    }

    /// <summary>
    /// Tests that GetPlatformFontNameManager returns an instance of SkiaPlatformResourceManager.
    /// Condition: Call GetPlatformFontNameManager.
    /// Expected Result: Returns an instance of SkiaPlatformResourceManager which implements IPlatformFontNameManager.
    /// </summary>
    [TestMethod]
    public void GetPlatformFontNameManager_WhenCalled_ReturnsSkiaPlatformResourceManagerInstance()
    {
        // Arrange
        var provider = new SkiaTextEditorPlatformProvider();
        var textEditor = new SkiaTextEditor(provider);

        // Act
        var result = provider.GetPlatformFontNameManager();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<SkiaPlatformResourceManager>(result);
    }

    /// <summary>
    /// Tests that GetFontLineSpacing throws when runProperty is null.
    /// Input: null runProperty
    /// Expected: NullReferenceException or ArgumentNullException
    /// </summary>
    [TestMethod]
    public void GetFontLineSpacing_NullRunProperty_ThrowsException()
    {
        // Arrange
        var provider = new SkiaTextEditorPlatformProvider();

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() => provider.GetFontLineSpacing(null!));
    }

    #region Helper Methods

    #endregion
}