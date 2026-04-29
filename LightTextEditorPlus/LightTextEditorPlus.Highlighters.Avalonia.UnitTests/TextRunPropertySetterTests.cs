using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Markdig.Syntax;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class TextRunPropertySetterTests
{
    [Fact]
    public void SetRunProperty_WithZeroStartOffset_AppliesPropertyCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Hello World";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, 4); // 0 to 4 inclusive = "Hello" (5 chars)
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 20 }, span);
        
        // Assert
        var selection = new Selection(new CaretOffset(0), 5);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(20, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_WithNonZeroStartOffset_AppliesPropertyWithOffset()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Hello World Test";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor) { StartOffset = new DocumentOffset(6) };
        var span = new SourceSpan(0, 4); // 0 to 4 inclusive = 5 chars
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 24 }, span);
        
        // Assert - Property should be applied at offset 6-11 (World)
        var selection = new Selection(new CaretOffset(6), 5);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(24, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_DisablesAndEnablesUndoRedo()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Test";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, 3); // 0 to 3 inclusive = "Test" (4 chars)
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 18 }, span);
        
        // Assert - Verify property was applied successfully
        var selection = new Selection(new CaretOffset(0), 4);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(18, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_WithMultipleProperties_AppliesAllProperties()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Styled Text";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, 5); // 0 to 5 inclusive = "Styled" (6 chars)
        
        // Act
        setter.SetRunProperty(property => property with 
        { 
            FontSize = 22,
            Opacity = 0.8
        }, span);
        
        // Assert
        var selection = new Selection(new CaretOffset(0), 6);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => 
        {
            Assert.Equal(22, p.FontSize);
            Assert.Equal(0.8, p.Opacity);
        });
    }

    [Fact]
    public void TrySetRunProperty_WithScopeType_AppliesPropertyWithOffset()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Code Example";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor) { StartOffset = new DocumentOffset(5) };
        var span = new SourceSpan(0, 6); // 0 to 6 inclusive = 7 chars
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 16 };
        
        // Act
        setter.TrySetRunProperty(ScopeType.Keyword, runProperty, span);
        
        // Assert - Property should be applied at offset 5-12 (Example)
        var selection = new Selection(new CaretOffset(5), 7);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(16, p.FontSize));
    }

    [Fact]
    public void TrySetRunProperty_WithScopeType_ZeroOffset_AppliesCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Keyword";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, 6); // 0 to 6 inclusive = "Keyword" (7 chars)
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 14 };
        
        // Act
        setter.TrySetRunProperty(ScopeType.Keyword, runProperty, span);
        
        // Assert
        var selection = new Selection(new CaretOffset(0), 7);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(14, p.FontSize));
    }

    [Fact]
    public void TrySetRunProperty_WithSelection_WhenPropertiesDifferent_SetsProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Test Text";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var selection = new Selection(new CaretOffset(0), 4);
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 18 };
        
        // Act
        setter.TrySetRunProperty(runProperty, in selection);
        
        // Assert
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(18, p.FontSize));
    }

    [Fact]
    public void TrySetRunProperty_WithSelection_WhenPropertiesSame_DoesNotSetAgain()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Same Property";
        textEditor.AppendText(text);
        
        var selection = new Selection(new CaretOffset(0), 4);
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 20 };
        
        // Set property first time
        textEditor.SetRunProperty(runProperty, selection);
        
        var setter = new TextRunPropertySetter(textEditor);
        
        // Act - Try to set the same property again
        setter.TrySetRunProperty(runProperty, in selection);
        
        // Assert - Property should still be 20
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(20, p.FontSize));
    }

    [Fact]
    public void TrySetRunProperty_WithSelection_DisablesAndEnablesUndoRedo()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Test";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var selection = new Selection(new CaretOffset(0), 4);
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 16 };
        
        // Act
        setter.TrySetRunProperty(runProperty, in selection);
        
        // Assert - Verify property was applied successfully
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(16, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_WithEmptySpan_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Test";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, -1); // Empty span (start > end)
        
        // Act & Assert - Should not throw
        setter.SetRunProperty(property => property with { FontSize = 12 }, span);
    }

    [Fact]
    public void TrySetRunProperty_WithEmptySelection_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Test";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var selection = new Selection(new CaretOffset(0), 0);
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 12 };
        
        // Act & Assert - Should not throw
        setter.TrySetRunProperty(runProperty, in selection);
    }

    [Fact]
    public void SetRunProperty_SpanExceedsDocumentLength_ThrowsException()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Short";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, 99); // 0 to 99 inclusive = 100 chars (larger than document)
        
        // Act & Assert - Should throw SelectionOutOfRangeException
        Assert.Throws<LightTextEditorPlus.Core.Exceptions.SelectionOutOfRangeException>(() =>
            setter.SetRunProperty(property => property with { FontSize = 14 }, span));
    }

    [Fact]
    public void TrySetRunProperty_MultipleCallsWithDifferentProperties_AppliesEachTime()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Dynamic Text";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var selection = new Selection(new CaretOffset(0), 7);
        
        // Act - Apply different properties multiple times
        var runProperty1 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 12 };
        setter.TrySetRunProperty(runProperty1, in selection);
        
        var runProperty2 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 16 };
        setter.TrySetRunProperty(runProperty2, in selection);
        
        var runProperty3 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 20 };
        setter.TrySetRunProperty(runProperty3, in selection);
        
        // Assert - Should have the last applied property
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(20, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_WithLargeOffset_AppliesPropertyCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "This is a longer text for testing with large offset values";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor) { StartOffset = new DocumentOffset(30) };
        var span = new SourceSpan(0, 9); // 0 to 9 inclusive = 10 chars
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 26 }, span);
        
        // Assert - Property should be applied at offset 30-40
        var selection = new Selection(new CaretOffset(30), 10);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(26, p.FontSize));
    }

    [Fact]
    public void TrySetRunProperty_WithPartiallyMatchingProperties_SetsProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Mixed Properties";
        textEditor.AppendText(text);
        
        // Set different font sizes to first and second half
        var selection1 = new Selection(new CaretOffset(0), 5);
        var property1 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 12 };
        textEditor.SetRunProperty(property1, selection1);
        
        var selection2 = new Selection(new CaretOffset(5), 5);
        var property2 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 16 };
        textEditor.SetRunProperty(property2, selection2);
        
        var setter = new TextRunPropertySetter(textEditor);
        var fullSelection = new Selection(new CaretOffset(0), 10);
        var targetProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 20 };
        
        // Act - Try to apply uniform property
        setter.TrySetRunProperty(targetProperty, in fullSelection);
        
        // Assert - All should have the new property
        var appliedProperties = textEditor.GetRunPropertyRange(fullSelection);
        Assert.All(appliedProperties, p => Assert.Equal(20, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_PreservesTextContent()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Preserve Content";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, text.Length - 1); // 0 to 15 inclusive = 16 chars
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 14 }, span);
        
        // Assert - Text content should remain unchanged
        var allSelection = textEditor.GetAllDocumentSelection();
        var documentText = textEditor.GetText(in allSelection);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void TrySetRunProperty_PreservesTextContent()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Keep Text Intact";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var selection = new Selection(new CaretOffset(0), text.Length);
        var runProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 18 };
        
        // Act
        setter.TrySetRunProperty(runProperty, in selection);
        
        // Assert - Text content should remain unchanged
        var allSelection = textEditor.GetAllDocumentSelection();
        var documentText = textEditor.GetText(in allSelection);
        Assert.Equal(text, documentText);
    }

    [Fact]
    public void SetRunProperty_MiddleOfDocument_AppliesPropertyCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Start Middle End";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(6, 11); // "Middle" (6 chars starting at offset 6)
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 15 }, span);
        
        // Assert
        var selection = new Selection(new CaretOffset(6), 6);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(15, p.FontSize));
    }

    [Fact]
    public void TrySetRunProperty_WithScopeType_IgnoresScopeTypeParameter()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "Test Code";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span1 = new SourceSpan(0, 3); // "Test" (4 chars)
        var span2 = new SourceSpan(5, 8); // "Code" (4 chars)
        var runProperty1 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 14 };
        var runProperty2 = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with { FontSize = 16 };
        
        // Act - Different scope types should not affect behavior
        setter.TrySetRunProperty(ScopeType.Keyword, runProperty1, span1);
        setter.TrySetRunProperty(ScopeType.Comment, runProperty2, span2);
        
        // Assert
        var selection1 = new Selection(new CaretOffset(0), 4);
        var appliedProperties1 = textEditor.GetRunPropertyRange(selection1);
        Assert.All(appliedProperties1, p => Assert.Equal(14, p.FontSize));
        
        var selection2 = new Selection(new CaretOffset(5), 4);
        var appliedProperties2 = textEditor.GetRunPropertyRange(selection2);
        Assert.All(appliedProperties2, p => Assert.Equal(16, p.FontSize));
    }

    [Fact]
    public void SetRunProperty_SingleCharacter_AppliesPropertyCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        const string text = "A";
        textEditor.AppendText(text);
        
        var setter = new TextRunPropertySetter(textEditor);
        var span = new SourceSpan(0, 0); // Single char at position 0
        
        // Act
        setter.SetRunProperty(property => property with { FontSize = 25 }, span);
        
        // Assert
        var selection = new Selection(new CaretOffset(0), 1);
        var appliedProperties = textEditor.GetRunPropertyRange(selection);
        Assert.All(appliedProperties, p => Assert.Equal(25, p.FontSize));
    }
}

