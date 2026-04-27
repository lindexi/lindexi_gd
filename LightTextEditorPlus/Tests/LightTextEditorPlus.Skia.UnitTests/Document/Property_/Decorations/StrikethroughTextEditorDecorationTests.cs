using System;
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


namespace LightTextEditorPlus.Document.Decorations.UnitTests;

/// <summary>
/// Tests for <see cref="StrikethroughTextEditorDecoration"/> class.
/// </summary>
[TestClass]
public class StrikethroughTextEditorDecorationTests
{
    /// <summary>
    /// Tests that the Instance property returns a non-null singleton instance.
    /// </summary>
    [TestMethod]
    public void Instance_ReturnsNonNullSingleton()
    {
        // Act
        var instance1 = StrikethroughTextEditorDecoration.Instance;
        var instance2 = StrikethroughTextEditorDecoration.Instance;

        // Assert
        Assert.IsNotNull(instance1);
        Assert.AreSame(instance1, instance2);
    }

}