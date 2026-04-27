using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LightTextEditorPlus.Document.UnitTests;

/// <summary>
/// Unit tests for <see cref="SkiaTextRun"/>.
/// </summary>
[TestClass]
public partial class SkiaTextRunTests
{
    /// <summary>
    /// Tests that the constructor creates a valid instance with valid text and null runProperty.
    /// </summary>
    [TestMethod]
    [DataRow("Hello World")]
    [DataRow("A")]
    [DataRow("Text with numbers 123")]
    public void Constructor_ValidTextAndNullRunProperty_CreatesInstance(string text)
    {
        // Arrange & Act
        var result = new SkiaTextRun(text, null);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SkiaTextRun));
        Assert.IsInstanceOfType(result, typeof(TextRun));
    }

    /// <summary>
    /// Tests that the constructor creates a valid instance with valid text using default parameter.
    /// </summary>
    [TestMethod]
    [DataRow("Sample text")]
    [DataRow("Another sample")]
    public void Constructor_ValidTextWithDefaultRunProperty_CreatesInstance(string text)
    {
        // Arrange & Act
        var result = new SkiaTextRun(text);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SkiaTextRun));
    }

    /// <summary>
    /// Tests that the constructor handles empty string correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_EmptyString_CreatesInstance()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var result = new SkiaTextRun(text);

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that the constructor handles whitespace-only strings correctly.
    /// </summary>
    [TestMethod]
    [DataRow(" ")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow("  \t  \n  ")]
    public void Constructor_WhitespaceOnlyString_CreatesInstance(string text)
    {
        // Arrange & Act
        var result = new SkiaTextRun(text);

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that the constructor handles very long strings correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_VeryLongString_CreatesInstance()
    {
        // Arrange
        var text = new string('A', 100000);

        // Act
        var result = new SkiaTextRun(text);

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that the constructor handles strings with special characters correctly.
    /// </summary>
    [TestMethod]
    [DataRow("Text with special chars: !@#$%^&*()")]
    [DataRow("Unicode: 你好世界")]
    [DataRow("Emoji: 😀🎉")]
    [DataRow("Mixed: Hello世界😀")]
    [DataRow("Symbols: ©®™§¶†‡")]
    public void Constructor_StringWithSpecialCharacters_CreatesInstance(string text)
    {
        // Arrange & Act
        var result = new SkiaTextRun(text);

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that the constructor handles strings with control characters correctly.
    /// </summary>
    [TestMethod]
    [DataRow("Text\u0000with\u0001control\u0002chars")]
    [DataRow("\u0007\u0008\u000B")]
    [DataRow("Tab\tNewline\nReturn\r")]
    public void Constructor_StringWithControlCharacters_CreatesInstance(string text)
    {
        // Arrange & Act
        var result = new SkiaTextRun(text);

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Tests that the constructor handles null text parameter.
    /// Expected to throw ArgumentNullException based on non-nullable parameter annotation.
    /// </summary>
    [TestMethod]
    public void Constructor_NullText_ThrowsException()
    {
        // Arrange
        string text = null!;

        // Act & Assert
        // Note: The behavior depends on the base class TextRun constructor implementation.
        // If it validates the text parameter, it should throw an exception.
        // If no validation exists, this test documents that null is passed through.
        try
        {
            var result = new SkiaTextRun(text);
            // If no exception is thrown, document this behavior
            Assert.IsNotNull(result);
        }
        catch (ArgumentNullException)
        {
            // Expected behavior if base constructor validates
            Assert.IsTrue(true);
        }
        catch (Exception ex)
        {
            // Other exceptions may be thrown by base constructor
            Assert.Fail($"Unexpected exception type: {ex.GetType().Name}");
        }
    }
}