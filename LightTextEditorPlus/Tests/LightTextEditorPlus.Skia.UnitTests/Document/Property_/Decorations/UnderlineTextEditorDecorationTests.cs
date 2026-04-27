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
/// Tests for <see cref="UnderlineTextEditorDecoration"/> class.
/// </summary>
[TestClass]
public partial class UnderlineTextEditorDecorationTests
{
    /// <summary>
    /// Tests that Instance property returns a valid singleton instance.
    /// Expected: Instance is not null and is the same instance on multiple accesses.
    /// </summary>
    [TestMethod]
    public void Instance_MultipleAccesses_ReturnsSameSingletonInstance()
    {
        // Act
        var instance1 = UnderlineTextEditorDecoration.Instance;
        var instance2 = UnderlineTextEditorDecoration.Instance;

        // Assert
        Assert.IsNotNull(instance1);
        Assert.AreSame(instance1, instance2);
    }

}