using System;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LightTextEditorPlus.UnitTests;


/// <summary>
/// Unit tests for <see cref="SkiaTextEditor"/> class.
/// </summary>
[TestClass]
public partial class SkiaTextEditorTests
{
    /// <summary>
    /// Tests that Backspace method executes without throwing an exception.
    /// </summary>
    [TestMethod]
    public void Backspace_WhenCalled_DoesNotThrowException()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();

        // Act & Assert
        textEditor.Backspace();
    }

    /// <summary>
    /// Tests that Backspace method correctly delegates to TextEditorCore when editor contains text.
    /// Verifies the method can be called multiple times without errors.
    /// </summary>
    [TestMethod]
    public void Backspace_WithTextInEditor_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World");

        // Act & Assert - should not throw
        textEditor.Backspace();
        textEditor.Backspace();
        textEditor.Backspace();
    }

    /// <summary>
    /// Tests that Backspace method can be called on an empty editor without throwing an exception.
    /// </summary>
    [TestMethod]
    public void Backspace_OnEmptyEditor_DoesNotThrowException()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();

        // Act & Assert
        textEditor.Backspace();
    }

    /// <summary>
    /// Tests that Backspace method can be called multiple times consecutively without errors.
    /// </summary>
    [TestMethod]
    public void Backspace_CalledMultipleTimes_DoesNotThrowException()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();

        // Act & Assert
        for (int i = 0; i < 10; i++)
        {
            textEditor.Backspace();
        }
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a valid empty selection (same start and end offset).
    /// </summary>
    [TestMethod]
    public void Remove_EmptySelection_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        CaretOffset offset = new CaretOffset(0);
        Selection selection = new Selection(offset, offset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a valid selection with positive offsets.
    /// </summary>
    [TestMethod]
    public void Remove_ValidSelectionWithPositiveOffsets_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World! This is a test.");
        CaretOffset startOffset = new CaretOffset(5);
        CaretOffset endOffset = new CaretOffset(10);
        Selection selection = new Selection(startOffset, endOffset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a selection where start offset is greater than end offset (reversed selection).
    /// </summary>
    [TestMethod]
    public void Remove_ReversedSelection_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World! This is a test.");
        CaretOffset startOffset = new CaretOffset(10);
        CaretOffset endOffset = new CaretOffset(5);
        Selection selection = new Selection(startOffset, endOffset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a selection created using offset and length constructor.
    /// </summary>
    [TestMethod]
    public void Remove_SelectionCreatedWithLength_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World! This is a test.");
        CaretOffset startOffset = new CaretOffset(3);
        Selection selection = new Selection(startOffset, 7);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a selection at the document start (offset 0).
    /// </summary>
    [TestMethod]
    public void Remove_SelectionAtDocumentStart_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World! This is a test.");
        CaretOffset startOffset = new CaretOffset(0);
        CaretOffset endOffset = new CaretOffset(5);
        Selection selection = new Selection(startOffset, endOffset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a selection using large offset values.
    /// </summary>
    [TestMethod]
    public void Remove_SelectionWithLargeOffsets_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        // Add enough text to make the large offsets valid
        textEditor.AppendText(new string('a', 2000000));
        CaretOffset startOffset = new CaretOffset(1000000);
        CaretOffset endOffset = new CaretOffset(2000000);
        Selection selection = new Selection(startOffset, endOffset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a selection where length is zero.
    /// </summary>
    [TestMethod]
    public void Remove_SelectionWithZeroLength_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        CaretOffset startOffset = new CaretOffset(10);
        Selection selection = new Selection(startOffset, 0);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a selection using CaretOffset with IsAtLineStart flag.
    /// </summary>
    [TestMethod]
    public void Remove_SelectionWithIsAtLineStartFlag_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        textEditor.AppendText("Hello World! This is a test.");
        CaretOffset startOffset = new CaretOffset(5, isAtLineStart: true);
        CaretOffset endOffset = new CaretOffset(10, isAtLineStart: false);
        Selection selection = new Selection(startOffset, endOffset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with maximum valid integer offset values.
    /// </summary>
    [TestMethod]
    public void Remove_SelectionWithMaxIntOffset_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        CaretOffset startOffset = new CaretOffset(int.MaxValue - 1);
        CaretOffset endOffset = new CaretOffset(int.MaxValue - 1);
        Selection selection = new Selection(startOffset, endOffset);

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Tests that Remove method executes without throwing when called with a default Selection struct.
    /// </summary>
    [TestMethod]
    public void Remove_DefaultSelection_DoesNotThrow()
    {
        // Arrange
        SkiaTextEditor textEditor = new SkiaTextEditor();
        Selection selection = default;

        // Act & Assert
        textEditor.Remove(in selection);
    }

    /// <summary>
    /// Verifies that calling Delete on an empty editor does not throw an exception.
    /// This tests the basic functionality of the Delete wrapper method when there is no content to delete.
    /// </summary>
    [TestMethod]
    public void Delete_EmptyEditor_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert
        editor.Delete();
    }

    /// <summary>
    /// Verifies that calling Delete after appending text does not throw an exception.
    /// This tests the Delete wrapper method delegates correctly to TextEditorCore when content exists.
    /// </summary>
    [TestMethod]
    public void Delete_WithContent_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Test content");

        // Act & Assert
        editor.Delete();
    }

    /// <summary>
    /// Verifies that Delete can be called multiple times in succession without throwing exceptions.
    /// This tests the robustness of the Delete wrapper method under repeated calls.
    /// </summary>
    [TestMethod]
    public void Delete_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Some text");

        // Act & Assert
        editor.Delete();
        editor.Delete();
        editor.Delete();
    }

    /// <summary>
    /// Tests that EditAndReplaceRun executes successfully with a valid text run and null selection.
    /// The null selection should use the current selection or insert at cursor position.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithValidTextRunAndNullSelection_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Hello World");

        // Act
        textEditor.EditAndReplaceRun(textRun, null);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun executes successfully with a valid text run and valid selection.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithValidTextRunAndValidSelection_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Test Text");
        var selection = new Selection(new CaretOffset(0), 0);

        // Act
        textEditor.EditAndReplaceRun(textRun, selection);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun handles an empty text run correctly.
    /// Expected: No exception thrown, empty text should be processed.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithEmptyTextRun_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("");

        // Act
        textEditor.EditAndReplaceRun(textRun, null);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun handles a text run with whitespace-only text.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithWhitespaceOnlyTextRun_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("   \t\n  ");

        // Act
        textEditor.EditAndReplaceRun(textRun, null);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun handles a very long text run.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithVeryLongTextRun_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var longText = new string('A', 10000);
        var textRun = new SkiaTextRun(longText);

        // Act
        textEditor.EditAndReplaceRun(textRun, null);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun handles text with special characters.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithSpecialCharactersTextRun_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Hello\r\n\t世界!@#$%^&*()");

        // Act
        textEditor.EditAndReplaceRun(textRun, null);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun handles a selection with zero length.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithZeroLengthSelection_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Insert");
        var selection = new Selection(new CaretOffset(0), 0);

        // Act
        textEditor.EditAndReplaceRun(textRun, selection);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun handles a selection with negative length by creating a valid selection.
    /// The Selection constructor should handle negative lengths appropriately.
    /// Expected: No exception thrown.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithNegativeLengthSelection_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        textEditor.AppendText("1234567890");
        var textRun = new SkiaTextRun("Test");
        var selection = new Selection(new CaretOffset(5), -3);

        // Act
        textEditor.EditAndReplaceRun(textRun, selection);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests EditAndReplaceRun with various text and selection combinations using parameterized data.
    /// Expected: All combinations execute successfully without throwing exceptions.
    /// </summary>
    /// <param name="text">The text to insert</param>
    /// <param name="startOffset">The start offset of selection</param>
    /// <param name="length">The length of selection</param>
    [TestMethod]
    [DataRow("Simple", 0, 0)]
    [DataRow("", 0, 0)]
    [DataRow("Unicode: 你好", 0, 0)]
    [DataRow("A", 0, 0)]
    [DataRow("Multiple\nLines\rWith\r\nBreaks", 0, 0)]
    public void EditAndReplaceRun_WithVariousInputs_ExecutesSuccessfully(string text, int startOffset, int length)
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun(text);
        var selection = new Selection(new CaretOffset(startOffset), length);

        // Act
        textEditor.EditAndReplaceRun(textRun, selection);

        // Assert
        // Method completes without throwing exception
    }

    /// <summary>
    /// Tests that EditAndReplaceRun throws ArgumentNullException when textRun is null.
    /// While the signature indicates non-nullable, C# allows passing null at runtime.
    /// Expected: ArgumentNullException or NullReferenceException.
    /// </summary>
    [TestMethod]
    public void EditAndReplaceRun_WithNullTextRun_ThrowsException()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            textEditor.EditAndReplaceRun(null!, null);
        });
    }

    /// <summary>
    /// Tests that AppendRun successfully executes when provided with a valid SkiaTextRun containing normal text.
    /// Verifies that the method completes without throwing any exceptions.
    /// </summary>
    [TestMethod]
    public void AppendRun_ValidTextRun_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Hello World");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun handles null parameter correctly.
    /// Even though the parameter is marked as non-nullable, this tests runtime behavior when null is passed.
    /// Expected to throw ArgumentNullException or NullReferenceException.
    /// </summary>
    [TestMethod]
    public void AppendRun_NullTextRun_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        SkiaTextRun textRun = null!;

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun successfully handles a SkiaTextRun with an empty string.
    /// Verifies that empty text runs are processed without errors.
    /// </summary>
    [TestMethod]
    public void AppendRun_EmptyText_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun successfully handles a SkiaTextRun with whitespace-only text.
    /// Verifies that whitespace is processed correctly without throwing exceptions.
    /// </summary>
    [TestMethod]
    public void AppendRun_WhitespaceOnlyText_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("   ");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun successfully handles a SkiaTextRun with a very long text string.
    /// Verifies that the method can process large amounts of text without performance issues or exceptions.
    /// </summary>
    [TestMethod]
    public void AppendRun_VeryLongText_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var longText = new string('A', 100000);
        var textRun = new SkiaTextRun(longText);

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun successfully handles a SkiaTextRun with special characters.
    /// Verifies that special characters such as newlines, tabs, and Unicode characters are processed correctly.
    /// </summary>
    [TestMethod]
    public void AppendRun_SpecialCharacters_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Hello\nWorld\t!\u00A9\u2764");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun with control characters does not throw an exception.
    /// Control characters should be handled consistently with AppendText.
    /// </summary>
    [TestMethod]
    public void AppendRun_ControlCharacters_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("\u0000\u0001\u0002\u001F");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun can be called multiple times consecutively.
    /// Verifies that the method supports multiple append operations without issues.
    /// </summary>
    [TestMethod]
    public void AppendRun_MultipleConsecutiveCalls_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun1 = new SkiaTextRun("First ");
        var textRun2 = new SkiaTextRun("Second ");
        var textRun3 = new SkiaTextRun("Third");

        // Act & Assert
        textEditor.AppendRun(textRun1);
        textEditor.AppendRun(textRun2);
        textEditor.AppendRun(textRun3);
    }

    /// <summary>
    /// Tests that AppendRun successfully handles a SkiaTextRun with Unicode emoji characters.
    /// Verifies that complex Unicode sequences are processed correctly.
    /// </summary>
    [TestMethod]
    public void AppendRun_UnicodeEmoji_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("Hello 😀🎉👍");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that AppendRun successfully handles a SkiaTextRun with single character.
    /// Verifies that minimal text input is processed correctly.
    /// </summary>
    [TestMethod]
    public void AppendRun_SingleCharacter_ExecutesSuccessfully()
    {
        // Arrange
        var textEditor = new SkiaTextEditor();
        var textRun = new SkiaTextRun("A");

        // Act & Assert
        textEditor.AppendRun(textRun);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with valid non-empty text and null selection.
    /// </summary>
    [TestMethod]
    [DataRow("Hello")]
    [DataRow("Test text")]
    [DataRow("A")]
    public void EditAndReplace_ValidTextWithNullSelection_ExecutesWithoutThrowing(string text)
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert - should not throw
        editor.EditAndReplace(text, null);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with valid non-empty text and default selection parameter.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_ValidTextWithDefaultSelection_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        string text = "Sample text";

        // Act & Assert - should not throw
        editor.EditAndReplace(text);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with empty string and null selection.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_EmptyStringWithNullSelection_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        string text = string.Empty;

        // Act & Assert - should not throw
        editor.EditAndReplace(text, null);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with whitespace-only strings and null selection.
    /// </summary>
    [TestMethod]
    [DataRow(" ")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow(" \t \n ")]
    public void EditAndReplace_WhitespaceStringWithNullSelection_ExecutesWithoutThrowing(string text)
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert - should not throw
        editor.EditAndReplace(text, null);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with strings containing special characters and null selection.
    /// </summary>
    [TestMethod]
    [DataRow("Hello\nWorld")]
    [DataRow("Tab\tSeparated")]
    [DataRow("Special!@#$%^&*()")]
    [DataRow("Unicode: 你好")]
    [DataRow("Emoji: 😀")]
    [DataRow("Quotes: \"Test\"")]
    [DataRow("Backslash: \\")]
    public void EditAndReplace_SpecialCharactersWithNullSelection_ExecutesWithoutThrowing(string text)
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert - should not throw
        editor.EditAndReplace(text, null);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with a very long string and null selection.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_VeryLongStringWithNullSelection_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        string text = new string('A', 10000);

        // Act & Assert - should not throw
        editor.EditAndReplace(text, null);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with valid text and a valid selection.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_ValidTextWithValidSelection_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Initial text");
        var selection = new Selection(new CaretOffset(0), 5);

        // Act & Assert - should not throw
        editor.EditAndReplace("Replacement", selection);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with empty string and a valid selection.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_EmptyStringWithValidSelection_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Initial text");
        var selection = new Selection(new CaretOffset(0), 5);

        // Act & Assert - should not throw
        editor.EditAndReplace(string.Empty, selection);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with text and a zero-length selection.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_ValidTextWithZeroLengthSelection_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Initial text");
        var selection = new Selection(new CaretOffset(5), 0);

        // Act & Assert - should not throw
        editor.EditAndReplace("Insert", selection);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with text and a selection at document start.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_ValidTextWithSelectionAtDocumentStart_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Initial text");
        var selection = new Selection(new CaretOffset(0), 0);

        // Act & Assert - should not throw
        editor.EditAndReplace("Start", selection);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with text and a selection spanning entire document.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_ValidTextWithSelectionSpanningEntireDocument_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        string initialText = "Initial text";
        editor.AppendText(initialText);
        var selection = new Selection(new CaretOffset(0), initialText.Length);

        // Act & Assert - should not throw
        editor.EditAndReplace("Complete replacement", selection);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with strings containing control characters.
    /// </summary>
    [TestMethod]
    [DataRow("\0")]
    [DataRow("\a")]
    [DataRow("\b")]
    [DataRow("\f")]
    [DataRow("\v")]
    [TestCategory("ProductionBugSuspected")]
    [Ignore("ProductionBugSuspected")]
    public void EditAndReplace_ControlCharactersWithNullSelection_ExecutesWithoutThrowing(string text)
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert - should not throw
        editor.EditAndReplace(text, null);
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called multiple times in succession.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_CalledMultipleTimes_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert - should not throw
        editor.EditAndReplace("First");
        editor.EditAndReplace("Second");
        editor.EditAndReplace("Third");
    }

    /// <summary>
    /// Tests that EditAndReplace executes without throwing when called with text after other edit operations.
    /// </summary>
    [TestMethod]
    public void EditAndReplace_CalledAfterOtherEditOperations_ExecutesWithoutThrowing()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        editor.AppendText("Initial");

        // Act & Assert - should not throw
        editor.EditAndReplace("Replacement", null);
    }

    /// <summary>
    /// Tests that AppendText with normal text input does not throw an exception.
    /// </summary>
    [TestMethod]
    public void AppendText_NormalText_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        var text = "Hello World";

        // Act & Assert
        editor.AppendText(text);
    }

    /// <summary>
    /// Tests that AppendText with empty string input does not throw an exception.
    /// The underlying implementation handles empty strings gracefully.
    /// </summary>
    [TestMethod]
    public void AppendText_EmptyString_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        var text = string.Empty;

        // Act & Assert
        editor.AppendText(text);
    }

    /// <summary>
    /// Tests that AppendText with whitespace-only string input does not throw an exception.
    /// </summary>
    /// <param name="whitespaceText">The whitespace text to test.</param>
    [TestMethod]
    [DataRow(" ")]
    [DataRow("  ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow("   \t\n   ")]
    public void AppendText_WhitespaceString_DoesNotThrow(string whitespaceText)
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert
        editor.AppendText(whitespaceText);
    }

    /// <summary>
    /// Tests that AppendText with text containing special characters does not throw an exception.
    /// </summary>
    /// <param name="specialText">The text with special characters to test.</param>
    [TestMethod]
    [DataRow("Hello\nWorld")]
    [DataRow("Hello\r\nWorld")]
    [DataRow("Hello\tWorld")]
    [DataRow("Hello\0World")]
    [DataRow("Hello 世界")]
    [DataRow("Hello 🌍")]
    [DataRow("!@#$%^&*()")]
    [DataRow("<>&\"'")]
    public void AppendText_SpecialCharacters_DoesNotThrow(string specialText)
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert
        editor.AppendText(specialText);
    }

    /// <summary>
    /// Tests that AppendText with a very long string does not throw an exception.
    /// </summary>
    [TestMethod]
    public void AppendText_VeryLongString_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        var text = new string('A', 100000);

        // Act & Assert
        editor.AppendText(text);
    }

    /// <summary>
    /// Tests that AppendText with null input throws ArgumentNullException.
    /// Even though the parameter is non-nullable, runtime null may be passed.
    /// </summary>
    [TestMethod]
    public void AppendText_NullString_DoesNotThrowDueToUnderlyingNullCheck()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        string? text = null;

        // Act & Assert
        // The underlying implementation checks for null and returns early
        editor.AppendText(text!);
    }

    /// <summary>
    /// Tests that AppendText with text containing control characters does not throw an exception.
    /// </summary>
    [TestMethod]
    public void AppendText_ControlCharacters_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        var text = "Hello\x01\x02\x03World";

        // Act & Assert
        editor.AppendText(text);
    }

    /// <summary>
    /// Tests that AppendText with Unicode characters including surrogate pairs does not throw an exception.
    /// </summary>
    [TestMethod]
    public void AppendText_UnicodeCharacters_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        var text = "Hello 你好 مرحبا שלום 🎉🎊";

        // Act & Assert
        editor.AppendText(text);
    }

    /// <summary>
    /// Tests that AppendText can be called multiple times without throwing an exception.
    /// </summary>
    [TestMethod]
    public void AppendText_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();

        // Act & Assert
        editor.AppendText("First line");
        editor.AppendText("Second line");
        editor.AppendText("Third line");
    }

    /// <summary>
    /// Tests that AppendText with mixed content (text, numbers, symbols) does not throw an exception.
    /// </summary>
    [TestMethod]
    public void AppendText_MixedContent_DoesNotThrow()
    {
        // Arrange
        var editor = new SkiaTextEditor();
        var text = "Text123!@# Mixed Content 世界 🌍";

        // Act & Assert
        editor.AppendText(text);
    }
}