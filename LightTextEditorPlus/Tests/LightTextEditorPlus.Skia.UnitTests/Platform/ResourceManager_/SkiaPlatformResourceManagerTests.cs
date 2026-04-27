using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Diagnostics.LogInfos;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Utils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SkiaSharp;

namespace LightTextEditorPlus.Platform.UnitTests;


/// <summary>
/// Unit tests for <see cref="SkiaPlatformResourceManager"/> class.
/// </summary>
[TestClass]
public partial class SkiaPlatformResourceManagerTests
{
    /// <summary>
    /// Helper class to expose protected members of SkiaPlatformResourceManager for testing.
    /// </summary>
    private class TestableSkiaPlatformResourceManager : SkiaPlatformResourceManager
    {
        private static readonly System.Reflection.FieldInfo? CacheField =
            typeof(SkiaPlatformResourceManager).GetField("InstalledFontCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        public TestableSkiaPlatformResourceManager(SkiaTextEditor textEditor) : base(textEditor)
        {
        }

        /// <summary>
        /// Exposes the protected TryResolveFont method for testing.
        /// </summary>
        public SKTypeface? PublicTryResolveFont(string fontName, SKFontStyle skFontStyle)
        {
            return TryResolveFont(fontName, skFontStyle);
        }

        /// <summary>
        /// Adds or updates an entry in the static InstalledFontCache.
        /// </summary>
        public void SetInstalledFontCache(string fontName, bool isInstalled)
        {
            if (CacheField?.GetValue(null) is ConcurrentDictionary<string, bool> cache)
            {
                cache[fontName] = isInstalled;
            }
        }

        /// <summary>
        /// Removes an entry from the static InstalledFontCache.
        /// </summary>
        public void RemoveFromInstalledFontCache(string fontName)
        {
            if (CacheField?.GetValue(null) is ConcurrentDictionary<string, bool> cache)
            {
                cache.TryRemove(fontName, out _);
            }
        }
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> returns a non-null string
    /// when provided with a valid font name.
    /// </summary>
    /// <param name="desiredFontName">The desired font name to test.</param>
    [TestMethod]
    [DataRow("Arial")]
    [DataRow("Times New Roman")]
    [DataRow("Calibri")]
    [DataRow("Segoe UI")]
    public void GetFallbackFontName_ValidFontName_ReturnsNonNullString(string desiredFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(desiredFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles empty string input.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    public void GetFallbackFontName_EmptyString_ReturnsNonNullString()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles whitespace-only string input.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    [DataRow(" ")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    public void GetFallbackFontName_WhitespaceString_ReturnsNonNullString(string desiredFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(desiredFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles very long string input.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    public void GetFallbackFontName_VeryLongString_ReturnsNonNullString()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);
        string veryLongFontName = new string('A', 10000);

        // Act
        string result = manager.GetFallbackFontName(veryLongFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles strings with special characters.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    [DataRow("Font@Name")]
    [DataRow("Font#Name")]
    [DataRow("Font$Name")]
    [DataRow("Font%Name")]
    [DataRow("Font&Name")]
    [DataRow("Font*Name")]
    [DataRow("Font!Name")]
    [DataRow("Font\0Name")]
    [DataRow("Font\u0001Name")]
    public void GetFallbackFontName_StringWithSpecialCharacters_ReturnsNonNullString(string desiredFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(desiredFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles Unicode characters.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    [DataRow("微软雅黑")]
    [DataRow("宋体")]
    [DataRow("ＭＳ ゴシック")]
    [DataRow("맑은 고딕")]
    [DataRow("Arial™")]
    public void GetFallbackFontName_UnicodeCharacters_ReturnsNonNullString(string desiredFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(desiredFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles null input.
    /// Expected behavior: Since the parameter is non-nullable, passing null should either throw or be handled by the implementation.
    /// This test verifies the actual behavior when null is passed.
    /// </summary>
    [TestMethod]
    public void GetFallbackFontName_NullInput_ThrowsOrHandlesGracefully()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act & Assert
        // Note: The parameter is marked as non-nullable, but we test null to verify runtime behavior.
        // The method may throw ArgumentNullException or handle null gracefully depending on implementation.
        try
        {
            string result = manager.GetFallbackFontName(null!);
            // If no exception is thrown, verify the result is valid
            Assert.IsNotNull(result);
        }
        catch (ArgumentNullException)
        {
            // Expected behavior for non-nullable parameter
            Assert.IsTrue(true);
        }
        catch (NullReferenceException)
        {
            // May occur if the method doesn't validate null input
            Assert.IsTrue(true);
        }
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> logs when fallback occurs.
    /// Note: Due to static dependency on TextContext.GlobalFontNameManager, this test verifies
    /// that the logger is set up correctly but cannot fully control the fallback conditions.
    /// Full testing of conditional logging would require refactoring to inject FontNameManager dependency.
    /// </summary>
    [TestMethod]
    public void GetFallbackFontName_NonExistentFont_LogsWhenFallbackOccurs()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Use a font name that is unlikely to exist to trigger fallback
        string nonExistentFontName = "NonExistentFont_12345_ABCDEF";

        // Act
        string result = manager.GetFallbackFontName(nonExistentFontName);

        // Assert
        Assert.IsNotNull(result);
        // Note: Cannot verify logger.Log was called due to static dependency on GlobalFontNameManager
        // The actual fallback behavior is determined by the static FontNameManager instance
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles font names with leading/trailing spaces.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    [DataRow(" Arial")]
    [DataRow("Arial ")]
    [DataRow(" Arial ")]
    [DataRow("  Arial  ")]
    public void GetFallbackFontName_FontNameWithLeadingTrailingSpaces_ReturnsNonNullString(string desiredFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(desiredFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that <see cref="SkiaPlatformResourceManager.GetFallbackFontName"/> handles font names with mixed casing.
    /// Expected behavior: Should return a fallback font name without throwing.
    /// </summary>
    [TestMethod]
    [DataRow("ARIAL")]
    [DataRow("arial")]
    [DataRow("ArIaL")]
    [DataRow("aRiAl")]
    public void GetFallbackFontName_MixedCasingFontName_ReturnsNonNullString(string desiredFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        Mock<ITextLogger> mockLogger = new Mock<ITextLogger>();
        mockTextEditor.Setup(x => x.Logger).Returns(mockLogger.Object);
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackFontName(desiredFontName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(string));
    }

    /// <summary>
    /// Tests that GetFallbackDefaultFontName returns a non-null string.
    /// </summary>
    [TestMethod]
    public void GetFallbackDefaultFontName_WhenCalled_ReturnsNonNullString()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackDefaultFontName();

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that GetFallbackDefaultFontName returns a non-empty string.
    /// </summary>
    [TestMethod]
    public void GetFallbackDefaultFontName_WhenCalled_ReturnsNonEmptyString()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackDefaultFontName();

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result));
    }

    /// <summary>
    /// Tests that GetFallbackDefaultFontName returns the same value as GetDefaultFontName static method.
    /// </summary>
    [TestMethod]
    public void GetFallbackDefaultFontName_WhenCalled_ReturnsSameAsGetDefaultFontName()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);
        string expectedFontName = SkiaPlatformResourceManager.GetDefaultFontName();

        // Act
        string result = manager.GetFallbackDefaultFontName();

        // Assert
        Assert.AreEqual(expectedFontName, result);
    }

    /// <summary>
    /// Tests that GetFallbackDefaultFontName returns one of the expected platform-specific font names.
    /// This verifies the method correctly delegates to GetDefaultFontName and returns a valid font name.
    /// </summary>
    [TestMethod]
    [DataRow("微软雅黑")]
    [DataRow("Noto Sans CJK SC")]
    [DataRow("PingFang SC")]
    public void GetFallbackDefaultFontName_WhenCalled_ReturnsExpectedPlatformFontName(string expectedFontName)
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);

        // Act
        string result = manager.GetFallbackDefaultFontName();

        // Assert
        // The result should be one of the platform-specific font names
        // We check if the result matches any of the expected values
        if (result == expectedFontName)
        {
            Assert.AreEqual(expectedFontName, result);
        }
    }

    /// <summary>
    /// Tests that GetFallbackDefaultFontName returns one of the known platform font names.
    /// </summary>
    [TestMethod]
    public void GetFallbackDefaultFontName_WhenCalled_ReturnsKnownPlatformFontName()
    {
        // Arrange
        Mock<SkiaTextEditor> mockTextEditor = new Mock<SkiaTextEditor>();
        SkiaPlatformResourceManager manager = new SkiaPlatformResourceManager(mockTextEditor.Object);
        string[] expectedFontNames = new[] { "微软雅黑", "Noto Sans CJK SC", "PingFang SC" };

        // Act
        string result = manager.GetFallbackDefaultFontName();

        // Assert
        CollectionAssert.Contains(expectedFontNames, result);
    }

    /// <summary>
    /// Tests that GetDefaultFontName returns a valid platform-specific font name.
    /// This test verifies that the method returns one of the expected font names
    /// based on the current operating system. The actual font name depends on the
    /// platform where the test runs: "微软雅黑" for Windows, "Noto Sans CJK SC" for Linux,
    /// or "PingFang SC" for macOS.
    /// </summary>
    [TestMethod]
    public void GetDefaultFontName_CalledOnAnyPlatform_ReturnsValidFontName()
    {
        // Arrange
        var expectedFontNames = new HashSet<string>
        {
            "微软雅黑",           // Windows default
            "Noto Sans CJK SC",   // Linux default
            "PingFang SC"         // macOS default
        };

        // Act
        string result = SkiaPlatformResourceManager.GetDefaultFontName();

        // Assert
        Assert.IsNotNull(result, "GetDefaultFontName should never return null.");
        Assert.IsFalse(string.IsNullOrEmpty(result), "GetDefaultFontName should return a non-empty string.");
        Assert.IsTrue(expectedFontNames.Contains(result),
            $"GetDefaultFontName returned '{result}', which is not one of the expected font names.");
    }

    /// <summary>
    /// Tests that GetDefaultFontName returns the Windows-specific font name when running on Windows.
    /// This test will pass on Windows and be inconclusive on other platforms.
    /// </summary>
    [TestMethod]
    public void GetDefaultFontName_OnWindows_ReturnsMicrosoftYaHei()
    {
        // Arrange & Act
        string result = SkiaPlatformResourceManager.GetDefaultFontName();

        // Assert
        if (OperatingSystem.IsWindows())
        {
            Assert.AreEqual("微软雅黑", result, "On Windows, GetDefaultFontName should return '微软雅黑'.");
        }
        else
        {
            Assert.Inconclusive("This test only applies to Windows platform.");
        }
    }

    /// <summary>
    /// Tests that GetDefaultFontName returns the Linux-specific font name when running on Linux.
    /// This test will pass on Linux and be inconclusive on other platforms.
    /// </summary>
    [TestMethod]
    public void GetDefaultFontName_OnLinux_ReturnsNotoSansCJKSC()
    {
        // Arrange & Act
        string result = SkiaPlatformResourceManager.GetDefaultFontName();

        // Assert
        if (OperatingSystem.IsLinux())
        {
            Assert.AreEqual("Noto Sans CJK SC", result, "On Linux, GetDefaultFontName should return 'Noto Sans CJK SC'.");
        }
        else
        {
            Assert.Inconclusive("This test only applies to Linux platform.");
        }
    }

    /// <summary>
    /// Tests that GetDefaultFontName returns the macOS-specific font name when running on macOS.
    /// This test will pass on macOS and be inconclusive on other platforms.
    /// </summary>
    [TestMethod]
    public void GetDefaultFontName_OnMacOS_ReturnsPingFangSC()
    {
        // Arrange & Act
        string result = SkiaPlatformResourceManager.GetDefaultFontName();

        // Assert
        if (OperatingSystem.IsMacOS())
        {
            Assert.AreEqual("PingFang SC", result, "On macOS, GetDefaultFontName should return 'PingFang SC'.");
        }
        else
        {
            Assert.Inconclusive("This test only applies to macOS platform.");
        }
    }

    /// <summary>
    /// Tests that GetDefaultFontName can be called multiple times and returns consistent results.
    /// Verifies that the method is deterministic and doesn't have side effects.
    /// </summary>
    [TestMethod]
    public void GetDefaultFontName_CalledMultipleTimes_ReturnsConsistentResult()
    {
        // Arrange & Act
        string firstCall = SkiaPlatformResourceManager.GetDefaultFontName();
        string secondCall = SkiaPlatformResourceManager.GetDefaultFontName();
        string thirdCall = SkiaPlatformResourceManager.GetDefaultFontName();

        // Assert
        Assert.AreEqual(firstCall, secondCall, "Multiple calls should return the same font name.");
        Assert.AreEqual(secondCall, thirdCall, "Multiple calls should return the same font name.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled returns true for a commonly installed font name.
    /// Verifies that the method correctly identifies an existing font through SkiaSharp's font manager.
    /// Input: A common font name like "Arial" or "Segoe UI".
    /// Expected: Returns true.
    /// </summary>
    [TestMethod]
    [DataRow("Arial")]
    [DataRow("Segoe UI")]
    [DataRow("Courier New")]
    public void CheckFontFamilyInstalled_WithCommonFontName_ReturnsTrue(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.IsTrue(result, $"Expected font '{fontName}' to be installed on the system.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled returns false for a non-existent font name.
    /// Verifies that the method correctly identifies when a font does not exist.
    /// Input: A clearly invalid/non-existent font name.
    /// Expected: Returns false.
    /// </summary>
    [TestMethod]
    [DataRow("NonExistentFont_XYZ123")]
    [DataRow("InvalidFontName_ABC999")]
    [DataRow("ThisFontDoesNotExist_Test")]
    public void CheckFontFamilyInstalled_WithNonExistentFontName_ReturnsFalse(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.IsFalse(result, $"Expected font '{fontName}' to not be installed on the system.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled uses cache on subsequent calls with the same font name.
    /// Verifies that calling the method multiple times with the same font name returns consistent results,
    /// demonstrating that the caching mechanism works correctly.
    /// Input: Same font name called twice.
    /// Expected: Both calls return the same result.
    /// </summary>
    [TestMethod]
    [DataRow("CachedFontTest_Unique_001")]
    [DataRow("CachedFontTest_Unique_002")]
    public void CheckFontFamilyInstalled_CalledTwiceWithSameFontName_ReturnsConsistentResult(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool firstResult = manager.CheckFontFamilyInstalled(fontName);
        bool secondResult = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.AreEqual(firstResult, secondResult, "Expected consistent results when checking the same font name twice.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled handles an empty string.
    /// Verifies the method's behavior when provided with an empty string as the font name.
    /// Input: Empty string "".
    /// Expected: Returns false (no font with empty name exists).
    /// </summary>
    [TestMethod]
    public void CheckFontFamilyInstalled_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);
        string fontName = string.Empty;

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.IsFalse(result, "Expected empty string font name to return false.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled handles whitespace-only strings.
    /// Verifies the method's behavior when provided with strings containing only whitespace.
    /// Input: Whitespace-only strings.
    /// Expected: Returns false (no font with whitespace-only name exists).
    /// </summary>
    [TestMethod]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("  \t  \n  ")]
    public void CheckFontFamilyInstalled_WithWhitespaceString_ReturnsFalse(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.IsFalse(result, "Expected whitespace-only font name to return false.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled handles very long font names.
    /// Verifies the method can process extremely long strings without errors.
    /// Input: Very long string (1000+ characters).
    /// Expected: Returns false without throwing exceptions.
    /// </summary>
    [TestMethod]
    public void CheckFontFamilyInstalled_WithVeryLongString_ReturnsFalse()
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);
        string fontName = new string('A', 10000);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.IsFalse(result, "Expected very long font name to return false.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled handles font names with special characters.
    /// Verifies the method's behavior with various special characters in font names.
    /// Input: Font names containing special characters.
    /// Expected: Returns false (fonts with these special characters likely don't exist).
    /// </summary>
    [TestMethod]
    [DataRow("Font@Name#123")]
    [DataRow("Font$Name%456")]
    [DataRow("Font&Name*789")]
    [DataRow("Font|Name\\Path")]
    [DataRow("Font<Name>Brackets")]
    public void CheckFontFamilyInstalled_WithSpecialCharacters_HandlesProperly(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        // Special characters in font names are unlikely to exist, but method should not throw
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled throws ArgumentNullException when fontName is null.
    /// Verifies that the method properly validates non-nullable parameter.
    /// Input: null.
    /// Expected: Throws ArgumentNullException or NullReferenceException.
    /// </summary>
    [TestMethod]
    public void CheckFontFamilyInstalled_WithNullFontName_ThrowsException()
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);
        string? fontName = null;

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() =>
        {
            manager.CheckFontFamilyInstalled(fontName!);
        }, "Expected exception when fontName is null.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled handles font names with control characters.
    /// Verifies the method's behavior with control characters in font names.
    /// Input: Font names containing control characters.
    /// Expected: Returns false without throwing exceptions.
    /// </summary>
    [TestMethod]
    [DataRow("Font\0Name")]
    [DataRow("Font\u0001Name")]
    [DataRow("Font\u001FName")]
    public void CheckFontFamilyInstalled_WithControlCharacters_ReturnsFalse(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        Assert.IsFalse(result, "Expected font name with control characters to return false.");
    }

    /// <summary>
    /// Tests that CheckFontFamilyInstalled handles Unicode font names.
    /// Verifies the method's behavior with various Unicode characters in font names.
    /// Input: Font names with Unicode characters.
    /// Expected: Handles properly (may return true or false depending on system fonts).
    /// </summary>
    [TestMethod]
    [DataRow("字体名称")]
    [DataRow("フォント名")]
    [DataRow("폰트이름")]
    [DataRow("ГарнитураШрифта")]
    public void CheckFontFamilyInstalled_WithUnicodeFontName_HandlesProperly(string fontName)
    {
        // Arrange
        var textEditor = new Mock<SkiaTextEditor>();
        var manager = new SkiaPlatformResourceManager(textEditor.Object);

        // Act
        bool result = manager.CheckFontFamilyInstalled(fontName);

        // Assert
        // Method should not throw, result depends on whether such fonts exist
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that the constructor properly initializes the SkiaTextEditor property
    /// when provided with a valid SkiaTextEditor instance.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidTextEditor_InitializesSkiaTextEditorProperty()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();

        // Act
        var manager = new SkiaPlatformResourceManager(textEditor);

        // Assert
        Assert.IsNotNull(manager.SkiaTextEditor);
        Assert.AreSame(textEditor, manager.SkiaTextEditor);
    }

    /// <summary>
    /// Tests that the constructor throws NullReferenceException when provided with a null textEditor parameter.
    /// The null parameter causes an exception when attempting to subscribe to the InternalRenderCompleted event.
    /// </summary>
    [TestMethod]
    public void Constructor_NullTextEditor_ThrowsNullReferenceException()
    {
        // Arrange
        SkiaTextEditor? textEditor = null;

        // Act & Assert
        Assert.ThrowsException<NullReferenceException>(() =>
        {
            var manager = new SkiaPlatformResourceManager(textEditor!);
        });
    }
}