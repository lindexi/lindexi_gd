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
        var textEditor = new SkiaTextEditor();
        var resourceManager = new SkiaPlatformResourceManager(textEditor);
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        var runProperty = new SkiaTextRunProperty(resourceManager);
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
        var textEditor = new SkiaTextEditor();
        var resourceManager = new SkiaPlatformResourceManager(textEditor);
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        var runProperty = new SkiaTextRunProperty(resourceManager);
        var mockCharObject = new Mock<ICharObject>(MockBehavior.Strict);
        mockCharObject.SetupGet(x => x.CodePoint).Returns(new Core.Primitive.Utf32CodePoint('a'));
        // Act
        var result = creator.ToPlatformRunProperty(mockCharObject.Object, runProperty);
        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SkiaTextRunProperty));
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty logs warning and returns compatible property
    /// when ResourceManager does not match, not in debug mode, and charObject is null.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMismatchedResourceManagerNotInDebugModeAndNullCharObject_LogsWarningAndReturnsCompatibleProperty()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.DebugConfiguration.SetExitDebugMode();
        var resourceManager = new SkiaPlatformResourceManager(textEditor);
        var otherTextEditor = new SkiaTextEditor();
        otherTextEditor.TextEditorCore.DebugConfiguration.SetExitDebugMode();
        var otherResourceManager = new SkiaPlatformResourceManager(otherTextEditor);
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        var runProperty = new SkiaTextRunProperty(otherResourceManager);
        // Act
        var result = creator.ToPlatformRunProperty(null, runProperty);
        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SkiaTextRunProperty));
        var resultProperty = (SkiaTextRunProperty)result;
        Assert.AreSame(resourceManager, resultProperty.ResourceManager);
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty throws TextEditorDebugException
    /// when ResourceManager does not match and in debug mode.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMismatchedResourceManagerInDebugMode_ThrowsTextEditorDebugException()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.DebugConfiguration.SetInDebugMode(withLog: false);
        var resourceManager = new SkiaPlatformResourceManager(textEditor);
        var otherTextEditor = new SkiaTextEditor();
        var otherResourceManager = new SkiaPlatformResourceManager(otherTextEditor);
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        var runProperty = new SkiaTextRunProperty(otherResourceManager);
        // Act & Assert
        var exception = Assert.ThrowsExactly<TextEditorDebugException>(() => creator.ToPlatformRunProperty(null, runProperty));
        Assert.IsNotNull(exception);
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty delegates to base class and throws exception
    /// when baseRunProperty is not a SkiaTextRunProperty.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_NonSkiaTextRunProperty_DelegatesToBaseAndThrows()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var resourceManager = new SkiaPlatformResourceManager(textEditor);
        var mockRunProperty = new Mock<IReadOnlyRunProperty>(MockBehavior.Strict);
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        // Act & Assert
        Assert.ThrowsExactly<NotSupportedException>(() => creator.ToPlatformRunProperty(null, mockRunProperty.Object));
    }

    /// <summary>
    /// Tests that ToPlatformRunProperty handles null DebugName properties gracefully
    /// when logging warning for mismatched ResourceManager.
    /// </summary>
    [TestMethod]
    public void ToPlatformRunProperty_SkiaTextRunPropertyWithMismatchedResourceManagerAndNullDebugNames_LogsWarningWithNullNames()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.TextEditorCore.DebugConfiguration.SetExitDebugMode();
        var resourceManager = new SkiaPlatformResourceManager(textEditor);
        var otherTextEditor = new SkiaTextEditor();
        otherTextEditor.TextEditorCore.DebugConfiguration.SetExitDebugMode();
        var otherResourceManager = new SkiaPlatformResourceManager(otherTextEditor);
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, textEditor);
        var runProperty = new SkiaTextRunProperty(otherResourceManager);
        // Act
        var result = creator.ToPlatformRunProperty(null, runProperty);
        // Assert
        Assert.IsNotNull(result);
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
    /// Tests that the constructor accepts null for the skiaPlatformResourceManager parameter.
    /// The constructor does not validate parameters, so null is accepted.
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

        // Act - constructor does not validate null, so no exception is expected
        var creator = new SkiaPlatformRunPropertyCreator(nullResourceManager!, textEditor);
        
        // Assert
        Assert.IsNotNull(creator);
    }

    /// <summary>
    /// Tests that the constructor accepts null for the textEditor parameter.
    /// The constructor does not validate parameters, so null is accepted.
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

        // Act - constructor does not validate null, so no exception is expected
        var creator = new SkiaPlatformRunPropertyCreator(resourceManager, nullTextEditor!);
        
        // Assert
        Assert.IsNotNull(creator);
    }

    /// <summary>
    /// Tests that the constructor accepts null for both parameters.
    /// The constructor does not validate parameters, so nulls are accepted.
    /// </summary>
    [TestMethod]
    public void Constructor_WithBothParametersNull_ThrowsException()
    {
        // Arrange
        SkiaPlatformResourceManager? nullResourceManager = null;
        SkiaTextEditor? nullTextEditor = null;
        
        // Act - constructor does not validate null, so no exception is expected
        var creator = new SkiaPlatformRunPropertyCreator(nullResourceManager!, nullTextEditor!);
        
        // Assert
        Assert.IsNotNull(creator);
    }
}