using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;
using LightTextEditorPlus.Primitive;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace LightTextEditorPlus.Document.Decorations.UnitTests;
/// <summary>
/// Tests for <see cref = "EmphasisDotsTextEditorDecoration"/> class.
/// </summary>
[TestClass]
public partial class EmphasisDotsTextEditorDecorationTests
{
    /// <summary>
    /// Tests that Instance property returns a singleton instance.
    /// </summary>
    [TestMethod]
    public void Instance_MultipleAccesses_ReturnsSameInstance()
    {
        // Act
        var instance1 = EmphasisDotsTextEditorDecoration.Instance;
        var instance2 = EmphasisDotsTextEditorDecoration.Instance;
        // Assert
        Assert.IsNotNull(instance1);
        Assert.AreSame(instance1, instance2);
    }

    /// <summary>
    /// Helper method to create a mock CharData with specified properties.
    /// Note: This creates a partial mock setup as CharData requires complex dependencies.
    /// The test focuses on the accessible properties needed by BuildDecoration.
    /// </summary>
    private CharData CreateMockCharData(double fontSize, TextRect bounds, SKColor? foregroundColor = null)
    {
        // Note: CharData constructor requires ICharObject and IReadOnlyRunProperty
        // Since we cannot create fakes, we need to provide minimal mock setup
        // This is a limitation that requires actual implementation or more complex mocking
        var mockCharObject = new Mock<ICharObject>();
        mockCharObject.Setup(c => c.ToText()).Returns("A");
        var mockRunProperty = new Mock<IReadOnlyRunProperty>();
        mockRunProperty.Setup(r => r.FontSize).Returns(fontSize);
        var charData = new CharData(mockCharObject.Object, mockRunProperty.Object);
        // Setup CharLayoutData to allow GetBounds to work
        // Note: This requires internal access which may not be possible in actual tests
        // The test may need to be marked as incomplete or use InternalsVisibleTo
        return charData;
    }

}