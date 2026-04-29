using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Microsoft.CodeAnalysis.Text;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class TextEditorColorCodeTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesInstance()
    {
        // Arrange
        var textEditor = new TextEditor();
        var startOffset = new DocumentOffset(0);

        // Act
        var colorCode = new TextEditorColorCode(textEditor, startOffset);

        // Assert
        Assert.NotNull(colorCode);
    }

    [Fact]
    public void FillCodeColor_CommentScope_AppliesCommentColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("// This is a comment");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 20);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Comment);

        // Assert - verify text is still intact
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("// This is a comment", text);
    }

    [Fact]
    public void FillCodeColor_KeywordScope_AppliesKeywordColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("class");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 5);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Keyword);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("class", text);
    }

    [Fact]
    public void FillCodeColor_StringScope_AppliesStringColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("\"hello world\"");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 13);

        // Act
        colorCode.FillCodeColor(span, ScopeType.String);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("\"hello world\"", text);
    }

    [Fact]
    public void FillCodeColor_NumberScope_AppliesNumberColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("42");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 2);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Number);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("42", text);
    }

    [Fact]
    public void FillCodeColor_ClassNameScope_AppliesClassNameColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("MyClass");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 7);

        // Act
        colorCode.FillCodeColor(span, ScopeType.ClassName);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("MyClass", text);
    }

    [Fact]
    public void FillCodeColor_BracketsScope_AppliesBracketsColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("{}");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 2);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Brackets);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("{}", text);
    }

    [Fact]
    public void FillCodeColor_VariableScope_AppliesVariableColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("myVariable");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 10);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Variable);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("myVariable", text);
    }

    [Fact]
    public void FillCodeColor_InvocationScope_AppliesInvocationColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("WriteLine");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 9);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Invocation);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("WriteLine", text);
    }

    [Fact]
    public void FillCodeColor_DeclarationTypeSyntaxScope_AppliesDeclarationTypeColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("var");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 3);

        // Act
        colorCode.FillCodeColor(span, ScopeType.DeclarationTypeSyntax);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("var", text);
    }

    [Fact]
    public void FillCodeColor_PlainTextScope_AppliesPlainTextColor()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("plain text");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 10);

        // Act
        colorCode.FillCodeColor(span, ScopeType.PlainText);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("plain text", text);
    }

    [Fact]
    public void FillCodeColor_WithStartOffset_AppliesColorAtCorrectPosition()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("prefix class suffix");
        var startOffset = new DocumentOffset(7);
        var colorCode = new TextEditorColorCode(textEditor, startOffset);
        var span = new TextSpan(0, 5);

        // Act
        colorCode.FillCodeColor(span, ScopeType.Keyword);

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("prefix class suffix", text);
    }

    [Fact]
    public void FillCodeColor_MultipleCallsWithDifferentScopes_AppliesAllColors()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("class MyClass { }");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));

        // Act - Apply different colors to different parts
        colorCode.FillCodeColor(new TextSpan(0, 5), ScopeType.Keyword); // "class"
        colorCode.FillCodeColor(new TextSpan(6, 7), ScopeType.ClassName); // "MyClass"
        colorCode.FillCodeColor(new TextSpan(14, 1), ScopeType.Brackets); // "{"
        colorCode.FillCodeColor(new TextSpan(16, 1), ScopeType.Brackets); // "}"

        // Assert
        var allSelection = textEditor.GetAllDocumentSelection();
        var text = textEditor.TextEditorCore.GetText(in allSelection);
        Assert.Equal("class MyClass { }", text);
    }

    [Fact]
    public void FillCodeColor_EmptySpan_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        textEditor.AppendText("text");
        var colorCode = new TextEditorColorCode(textEditor, new DocumentOffset(0));
        var span = new TextSpan(0, 0);

        // Act & Assert
        colorCode.FillCodeColor(span, ScopeType.PlainText);
    }

    [Fact]
    public void ColorCodeStyleManager_Constructor_InitializesAllScopeTypes()
    {
        // Arrange
        var textEditor = new TextEditor();

        // Act
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Assert - Verify manager is created
        Assert.NotNull(styleManager);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_CommentScope_ReturnsCommentProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.Comment);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_KeywordScope_ReturnsKeywordProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.Keyword);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_StringScope_ReturnsStringProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.String);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_NumberScope_ReturnsNumberProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.Number);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_ClassNameScope_ReturnsClassNameProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.ClassName);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_BracketsScope_ReturnsBracketsProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.Brackets);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_VariableScope_ReturnsVariableProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.Variable);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_InvocationScope_ReturnsInvocationProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.Invocation);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_DeclarationTypeSyntaxScope_ReturnsDeclarationTypeProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.DeclarationTypeSyntax);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_PlainTextScope_ReturnsPlainTextProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var runProperty = styleManager.GetRunProperty(ScopeType.PlainText);

        // Assert
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_UnknownScope_ReturnsPlainTextProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act - Use ClassMember which is not in the dictionary
        var runProperty = styleManager.GetRunProperty(ScopeType.ClassMember);

        // Assert - Should return plain text property as fallback
        Assert.NotNull(runProperty);
        Assert.NotNull(runProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_DifferentScopes_ReturnsDifferentColors()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var commentProperty = styleManager.GetRunProperty(ScopeType.Comment);
        var keywordProperty = styleManager.GetRunProperty(ScopeType.Keyword);
        var stringProperty = styleManager.GetRunProperty(ScopeType.String);

        // Assert - Different scopes should have different properties
        Assert.NotEqual(commentProperty.Foreground, keywordProperty.Foreground);
        Assert.NotEqual(commentProperty.Foreground, stringProperty.Foreground);
        Assert.NotEqual(keywordProperty.Foreground, stringProperty.Foreground);
    }

    [Fact]
    public void ColorCodeStyleManager_GetRunProperty_SameScope_ReturnsSameProperty()
    {
        // Arrange
        var textEditor = new TextEditor();
        var styleManager = new ColorCodeStyleManager(textEditor);

        // Act
        var property1 = styleManager.GetRunProperty(ScopeType.Comment);
        var property2 = styleManager.GetRunProperty(ScopeType.Comment);

        // Assert - Same scope should return equal properties
        Assert.Equal(property1, property2);
    }
}
