using LightTextEditorPlus.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;


namespace LightTextEditorPlus.Document.UnitTests;

/// <summary>
/// Unit tests for <see cref="CacheRenderingRunPropertyInfo"/>.
/// </summary>
[TestClass]
public class CacheRenderingRunPropertyInfoTests
{
    /// <summary>
    /// Tests that Dispose properly disposes all non-null properties.
    /// Tests with combinations of null and non-null Font and Paint.
    /// Expected: Typeface is always disposed, Font and Paint are disposed only when non-null.
    /// </summary>
    /// <param name="createFont">Whether to create a non-null Font.</param>
    /// <param name="createPaint">Whether to create a non-null Paint.</param>
    [TestMethod]
    [DataRow(true, true, DisplayName = "Dispose_AllPropertiesNonNull_DisposesAllObjects")]
    [DataRow(true, false, DisplayName = "Dispose_FontNonNullPaintNull_DisposesTypefaceAndFont")]
    [DataRow(false, true, DisplayName = "Dispose_FontNullPaintNonNull_DisposesTypefaceAndPaint")]
    [DataRow(false, false, DisplayName = "Dispose_FontAndPaintNull_DisposesTypefaceOnly")]
    public void Dispose_VariousNullCombinations_DisposesNonNullPropertiesCorrectly(bool createFont, bool createPaint)
    {
        // Arrange
        var typeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.CreateDefault();
        var font = createFont ? new SKFont(typeface, 12) : null;
        var paint = createPaint ? new SKPaint() : null;

        var info = new CacheRenderingRunPropertyInfo(typeface, font, paint);

        // Act
        info.Dispose();

        // Assert
        // Verify Typeface is disposed by checking its Handle
        Assert.AreEqual(IntPtr.Zero, typeface.Handle, "Typeface should be disposed.");

        if (createFont)
        {
            Assert.AreEqual(IntPtr.Zero, font!.Handle, "Font should be disposed when non-null.");
        }

        if (createPaint)
        {
            Assert.AreEqual(IntPtr.Zero, paint!.Handle, "Paint should be disposed when non-null.");
        }
    }

    /// <summary>
    /// Tests that Dispose does not throw when Font is null.
    /// Expected: No exception is thrown.
    /// </summary>
    [TestMethod]
    public void Dispose_FontIsNull_DoesNotThrowException()
    {
        // Arrange
        var typeface = SKTypeface.Default;
        var info = new CacheRenderingRunPropertyInfo(typeface, null, new SKPaint());

        // Act & Assert
        info.Dispose(); // Should not throw
    }

    /// <summary>
    /// Tests that Dispose does not throw when Paint is null.
    /// Expected: No exception is thrown.
    /// </summary>
    [TestMethod]
    public void Dispose_PaintIsNull_DoesNotThrowException()
    {
        // Arrange
        var typeface = SKTypeface.Default;
        var info = new CacheRenderingRunPropertyInfo(typeface, new SKFont(SKTypeface.Default, 12), null);

        // Act & Assert
        info.Dispose(); // Should not throw
    }

    /// <summary>
    /// Tests that Dispose does not throw when both Font and Paint are null.
    /// Expected: No exception is thrown, only Typeface is disposed.
    /// </summary>
    [TestMethod]
    public void Dispose_BothFontAndPaintNull_DoesNotThrowException()
    {
        // Arrange
        var typeface = SKTypeface.Default;
        var info = new CacheRenderingRunPropertyInfo(typeface, null, null);

        // Act & Assert
        info.Dispose(); // Should not throw
    }

    /// <summary>
    /// Tests that Dispose can be called multiple times without throwing.
    /// Expected: Multiple calls to Dispose do not throw exceptions.
    /// </summary>
    [TestMethod]
    public void Dispose_CalledMultipleTimes_DoesNotThrowException()
    {
        // Arrange
        var typeface = SKTypeface.Default;
        var font = new SKFont(SKTypeface.Default, 12);
        var paint = new SKPaint();
        var info = new CacheRenderingRunPropertyInfo(typeface, font, paint);

        // Act & Assert
        info.Dispose();
        info.Dispose(); // Should not throw on second call
    }
}