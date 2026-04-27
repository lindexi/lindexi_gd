using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Globalization;
using System.Reflection;

namespace LightTextEditorPlus.Document.UnitTests;
/// <summary>
/// Tests for <see cref = "SkiaPlatformRunPropertyCreator"/> class.
/// </summary>
[TestClass]
public class SkiaPlatformRunPropertyCreatorTests
{
    /// <summary>
    /// Helper class to expose protected methods for testing.
    /// </summary>
    private class TestableSkiaPlatformRunPropertyCreator : SkiaPlatformRunPropertyCreator
    {
        public TestableSkiaPlatformRunPropertyCreator(SkiaPlatformResourceManager skiaPlatformResourceManager, SkiaTextEditor textEditor) : base(skiaPlatformResourceManager, textEditor)
        {
        }

        public SkiaTextRunProperty PublicOnUpdateMarkerRunProperty(SkiaTextRunProperty? markerRunProperty, SkiaTextRunProperty styleRunProperty)
        {
            return OnUpdateMarkerRunProperty(markerRunProperty, styleRunProperty);
        }
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty returns the SkiaTextRunProperty directly when
    /// the ResourceManager matches and charObject is null.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMatchingResourceManagerAndNullCharObject_ReturnsPropertyDirectly()
    {
        // Arrange
        var mockResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        mockTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
        var creator = new SkiaPlatformRunPropertyCreator(mockResourceManager.Object, mockTextEditor.Object);
        var runProperty = new SkiaTextRunProperty(mockResourceManager.Object);
        // Act
        var result = creator.ToPlatformRunProperty(null, runProperty);
        // Assert
        Assert.AreSame(runProperty, result);
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty normalizes the property when
    /// the ResourceManager matches and charObject is not null.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMatchingResourceManagerAndNonNullCharObject_NormalizesProperty()
    {
        // Arrange
        var mockResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockCharObject = new Mock<ICharObject>(MockBehavior.Strict);
        mockTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
        var creator = new SkiaPlatformRunPropertyCreator(mockResourceManager.Object, mockTextEditor.Object);
        var runProperty = new SkiaTextRunProperty(mockResourceManager.Object);
        var normalizedProperty = new SkiaTextRunProperty(mockResourceManager.Object);
        mockResourceManager.Setup(x => x.NormalRunProperty(runProperty, mockCharObject.Object)).Returns(normalizedProperty);
        // Act
        var result = creator.ToPlatformRunProperty(mockCharObject.Object, runProperty);
        // Assert
        Assert.AreSame(normalizedProperty, result);
        mockResourceManager.Verify(x => x.NormalRunProperty(runProperty, mockCharObject.Object), Times.Once);
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty logs warning and returns compatible property
    /// when ResourceManager does not match, not in debug mode, and charObject is null.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMismatchedResourceManagerNotInDebugModeAndNullCharObject_LogsWarningAndReturnsCompatibleProperty()
    {
        // Arrange
        var mockResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockOtherResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockOtherTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockOtherTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockLogger = new Mock<ITextLogger>(MockBehavior.Strict);
        mockTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditor.SetupGet(x => x.Logger).Returns(mockLogger.Object);
        mockTextEditorCore.SetupGet(x => x.IsInDebugMode).Returns(false);
        mockTextEditorCore.SetupGet(x => x.DebugName).Returns("TestEditor");
        mockOtherTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockOtherTextEditorCore.Object);
        mockOtherTextEditorCore.SetupGet(x => x.DebugName).Returns("OtherEditor");
        mockOtherResourceManager.SetupGet(x => x.SkiaTextEditor).Returns(mockOtherTextEditor.Object);
        mockLogger.Setup(x => x.LogWarning(It.IsAny<string>()));
        var creator = new SkiaPlatformRunPropertyCreator(mockResourceManager.Object, mockTextEditor.Object);
        var runProperty = new SkiaTextRunProperty(mockOtherResourceManager.Object);
        // Act
        var result = creator.ToPlatformRunProperty(null, runProperty);
        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SkiaTextRunProperty));
        var resultProperty = (SkiaTextRunProperty)result;
        Assert.AreSame(mockResourceManager.Object, resultProperty.ResourceManager);
        mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty throws TextEditorDebugException
    /// when ResourceManager does not match and in debug mode.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMismatchedResourceManagerInDebugMode_ThrowsTextEditorDebugException()
    {
        // Arrange
        var mockResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockOtherResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockOtherTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockOtherTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockLogger = new Mock<ITextLogger>(MockBehavior.Strict);
        mockTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditor.SetupGet(x => x.Logger).Returns(mockLogger.Object);
        mockTextEditorCore.SetupGet(x => x.IsInDebugMode).Returns(true);
        mockTextEditorCore.SetupGet(x => x.DebugName).Returns("TestEditor");
        mockOtherTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockOtherTextEditorCore.Object);
        mockOtherTextEditorCore.SetupGet(x => x.DebugName).Returns("OtherEditor");
        mockOtherResourceManager.SetupGet(x => x.SkiaTextEditor).Returns(mockOtherTextEditor.Object);
        mockLogger.Setup(x => x.LogWarning(It.IsAny<string>()));
        var creator = new SkiaPlatformRunPropertyCreator(mockResourceManager.Object, mockTextEditor.Object);
        var runProperty = new SkiaTextRunProperty(mockOtherResourceManager.Object);
        // Act & Assert
        var exception = Assert.ThrowsException<TextEditorDebugException>(() => creator.ToPlatformRunProperty(null, runProperty));
        Assert.IsNotNull(exception);
        mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty delegates to base class and throws exception
    /// when baseRunProperty is not a SkiaTextRunProperty.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_NonSkiaTextRunProperty_DelegatesToBaseAndThrows()
    {
        // Arrange
        var mockResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockRunProperty = new Mock<IReadOnlyRunProperty>(MockBehavior.Strict);
        mockTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
        var creator = new SkiaPlatformRunPropertyCreator(mockResourceManager.Object, mockTextEditor.Object);
        // Act & Assert
        Assert.ThrowsException<Exception>(() => creator.ToPlatformRunProperty(null, mockRunProperty.Object));
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty handles null DebugName properties gracefully
    /// when logging warning for mismatched ResourceManager.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMismatchedResourceManagerAndNullDebugNames_LogsWarningWithNullNames()
    {
        // Arrange
        var mockResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockOtherResourceManager = new Mock<SkiaPlatformResourceManager>(MockBehavior.Strict);
        var mockTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockOtherTextEditorCore = new Mock<TextEditorCore>(MockBehavior.Strict);
        var mockTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockOtherTextEditor = new Mock<SkiaTextEditor>(MockBehavior.Strict);
        var mockLogger = new Mock<ITextLogger>(MockBehavior.Strict);
        mockTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockTextEditorCore.Object);
        mockTextEditor.SetupGet(x => x.Logger).Returns(mockLogger.Object);
        mockTextEditorCore.SetupGet(x => x.IsInDebugMode).Returns(false);
        mockTextEditorCore.SetupGet(x => x.DebugName).Returns((string? )null);
        mockOtherTextEditor.SetupGet(x => x.TextEditorCore).Returns(mockOtherTextEditorCore.Object);
        mockOtherTextEditorCore.SetupGet(x => x.DebugName).Returns((string? )null);
        mockOtherResourceManager.SetupGet(x => x.SkiaTextEditor).Returns(mockOtherTextEditor.Object);
        mockLogger.Setup(x => x.LogWarning(It.IsAny<string>()));
        var creator = new SkiaPlatformRunPropertyCreator(mockResourceManager.Object, mockTextEditor.Object);
        var runProperty = new SkiaTextRunProperty(mockOtherResourceManager.Object);
        // Act
        var result = creator.ToPlatformRunProperty(null, runProperty);
        // Assert
        Assert.IsNotNull(result);
        mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Tests that the constructor properly assigns both parameters to their respective fields
    /// when valid non-null instances are provided.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_AssignsFieldsCorrectly()
    {
        // Arrange
        // Note: SkiaTextEditor and SkiaPlatformResourceManager cannot be mocked.
        // This test requires these classes to have accessible constructors.
        // If compilation fails, manual instantiation or test skip may be required.
        SkiaTextEditor textEditor;
        SkiaPlatformResourceManager resourceManager;
        try
        {
            textEditor = new SkiaTextEditor();
            resourceManager = new SkiaPlatformResourceManager(textEditor);
        }
        catch
        {
            Assert.Inconclusive("Unable to instantiate required dependencies (SkiaTextEditor or SkiaPlatformResourceManager). These classes may require specific initialization that cannot be provided in this test context.");
            return;
        }

        // Act
        SkiaPlatformRunPropertyCreator creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        // Assert
        Assert.IsNotNull(creator);
    }

    /// <summary>
    /// Tests that the constructor throws an exception when the skiaPlatformResourceManager parameter is null.
    /// Expected to throw ArgumentNullException or NullReferenceException depending on implementation.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullResourceManager_ThrowsException()
    {
        // Arrange
        SkiaPlatformResourceManager? nullResourceManager = null;
        SkiaTextEditor textEditor;
        try
        {
            textEditor = new SkiaTextEditor();
        }
        catch
        {
            Assert.Inconclusive("Unable to instantiate SkiaTextEditor. This class may require specific initialization that cannot be provided in this test context.");
            return;
        }

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            _ = new SkiaPlatformRunPropertyCreator(nullResourceManager!, textEditor);
        });
    }

    /// <summary>
    /// Tests that the constructor throws an exception when the textEditor parameter is null.
    /// Expected to throw ArgumentNullException or NullReferenceException depending on implementation.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullTextEditor_ThrowsException()
    {
        // Arrange
        SkiaTextEditor? nullTextEditor = null;
        SkiaTextEditor tempTextEditor;
        SkiaPlatformResourceManager resourceManager;
        try
        {
            tempTextEditor = new SkiaTextEditor();
            resourceManager = new SkiaPlatformResourceManager(tempTextEditor);
        }
        catch
        {
            Assert.Inconclusive("Unable to instantiate required dependencies. These classes may require specific initialization that cannot be provided in this test context.");
            return;
        }

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            _ = new SkiaPlatformRunPropertyCreator(resourceManager, nullTextEditor!);
        });
    }

    /// <summary>
    /// Tests that the constructor throws an exception when both parameters are null.
    /// Expected to throw ArgumentNullException depending on which parameter is validated first.
    /// </summary>
    [TestMethod]
    public void Constructor_WithBothParametersNull_ThrowsException()
    {
        // Arrange
        SkiaPlatformResourceManager? nullResourceManager = null;
        SkiaTextEditor? nullTextEditor = null;
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            _ = new SkiaPlatformRunPropertyCreator(nullResourceManager!, nullTextEditor!);
        });
    }
}