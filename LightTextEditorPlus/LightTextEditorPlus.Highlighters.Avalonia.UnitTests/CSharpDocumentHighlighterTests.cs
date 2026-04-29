using ColorCode.Common;
using LightTextEditorPlus;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class CSharpDocumentHighlighterTests
{
    [Fact]
    public void Constructor_NullTextEditor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CSharpDocumentHighlighter(null!));
    }

    [Fact]
    public void Constructor_ValidTextEditor_CreatesInstance()
    {
        var textEditor = new TextEditor();

        var highlighter = new CSharpDocumentHighlighter(textEditor);

        Assert.NotNull(highlighter);
    }

    [Fact]
    public void ApplyHighlight_EmptyString_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);

        highlighter.ApplyHighlight(string.Empty);
    }

    [Fact]
    public void ApplyHighlight_PlainText_AppliesPlainTextHighlighting()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "hello world";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_KeywordOnly_HighlightsKeyword()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "class";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "class", ScopeType.Keyword);
    }

    [Fact]
    public void ApplyHighlight_SimpleClassDeclaration_HighlightsKeywordsAndClassName()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "public class MyClass { }";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "public", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "class", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "MyClass", ScopeType.ClassName);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_StringLiteral_HighlightsString()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var text = \"hello\";";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "var", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "\"hello\"", ScopeType.String);
    }

    [Fact]
    public void ApplyHighlight_NumericLiteral_HighlightsNumber()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "int number = 42;";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "int", ScopeType.Keyword, ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "42", ScopeType.Number);
    }

    [Fact]
    public void ApplyHighlight_SingleLineComment_HighlightsComment()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "// This is a comment";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertTokenUsesNonPlainTextColor(textEditor, text, "//");
    }

    [Fact]
    public void ApplyHighlight_MultiLineComment_HighlightsComment()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "/* Multi\nline\ncomment */";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_MethodInvocation_HighlightsMethodName()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "Console.WriteLine(\"test\");";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "WriteLine", ScopeType.Invocation);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "\"test\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "(", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ")", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_VariableDeclaration_HighlightsVariableName()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "int myVariable = 10;";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "int", ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "myVariable", ScopeType.Variable);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "10", ScopeType.Number);
    }

    [Fact]
    public void ApplyHighlight_ComplexCode_HighlightsAllElements()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "public class MyClass { private int _value = 42; // comment\n public void DoSomething() { var text = \"hello\"; Console.WriteLine(text); } }";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "public", ScopeType.Keyword, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "class", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "MyClass", ScopeType.ClassName);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "private", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "_value", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "42", ScopeType.Number);
        DocumentHighlighterTestHelper.AssertTokenUsesNonPlainTextColor(textEditor, text, "// comment");
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "DoSomething", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "var", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "\"hello\"", ScopeType.String);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "WriteLine", ScopeType.Invocation);
    }

    [Fact]
    public void ApplyHighlight_InterpolatedString_HighlightsString()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var message = $\"Value: {value}\";";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "var", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_Brackets_HighlightsBrackets()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var array = new int[] { 1, 2, 3 };";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "var", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "new", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "[", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "]", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "}", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_GenericType_HighlightsBrackets()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var list = new List<string>();";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "<", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ">", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "(", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ")", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_LocalVariableWithType_HighlightsType()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "string myText = \"test\";";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "string", ScopeType.Keyword, ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "myText", ScopeType.Variable);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "\"test\"", ScopeType.String);
    }

    [Fact]
    public void ApplyHighlight_VarKeyword_HighlightsAsKeyword()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var value = 10;";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "var", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "value", ScopeType.Variable);
    }

    [Fact]
    public void ApplyHighlight_MultipleKeywords_HighlightsAll()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "public static void Main() { return; }";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "public", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "static", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "void", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "return", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "Main", ScopeType.ClassMember);
    }

    [Fact]
    public void RenderBackground_ValidContext_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        var mockDrawingContext = new Mock<global::Avalonia.Media.DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        highlighter.RenderBackground(in context);
    }

    [Fact]
    public void RenderForeground_ValidContext_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        var mockDrawingContext = new Mock<global::Avalonia.Media.DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object)
        {
            Viewport = null
        };

        highlighter.RenderForeground(in context);
    }

    [Fact]
    public void ApplyHighlight_CalledMultipleTimes_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var x = 10;";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);
        highlighter.ApplyHighlight(text);
        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "var", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "x", ScopeType.Variable);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "10", ScopeType.Number);
    }

    [Fact]
    public void ApplyHighlight_DifferentCodeEachTime_UpdatesHighlighting()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);

        const string text1 = "var x = 10;";
        textEditor.AppendText(text1);
        highlighter.ApplyHighlight(text1);
        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text1);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text1, "var", ScopeType.Keyword);

#pragma warning disable CS0618
        textEditor.TextEditorCore.Clear();
#pragma warning restore CS0618
        const string text2 = "class MyClass { }";
        textEditor.AppendText(text2);
        highlighter.ApplyHighlight(text2);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text2);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text2, "class", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text2, "MyClass", ScopeType.ClassName);
    }

    [Fact]
    public void ApplyHighlight_CharacterLiteral_HighlightsAsString()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "char c = 'a';";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "char", ScopeType.Keyword, ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "'a'", ScopeType.String, ScopeType.ClassName);
    }

    [Fact]
    public void ApplyHighlight_DocumentationComment_HighlightsAsComment()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "/// <summary>Documentation</summary>";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
    }

    [Fact]
    public void ApplyHighlight_PropertyDeclaration_HighlightsCorrectly()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "public string Name { get; set; }";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "public", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "string", ScopeType.Keyword, ScopeType.DeclarationTypeSyntax);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "Name", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "get", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "set", ScopeType.Keyword);
    }

    [Fact]
    public void ApplyHighlight_FieldDeclaration_HighlightsCorrectly()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "private readonly int _count;";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "private", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "readonly", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "int", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "_count", ScopeType.ClassMember);
    }

    [Fact]
    public void ApplyHighlight_ConstructorDeclaration_HighlightsCorrectly()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "public MyClass() { }";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "public", ScopeType.Keyword);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "MyClass", ScopeType.ClassMember);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "(", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ")", ScopeType.Brackets);
    }

    [Fact]
    public void ApplyHighlight_ForEachLoop_HighlightsVariableAndKeyword()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "foreach (var item in items) { }";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertDocumentContainsNonPlainTextColor(textEditor);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "item", ScopeType.Variable);
    }

    [Fact]
    public void ApplyHighlight_WhitespaceOnly_DoesNotThrow()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "   \t\n  ";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
    }

    [Fact]
    public void ApplyHighlight_NestedBrackets_HighlightsAllBrackets()
    {
        var textEditor = new TextEditor();
        var highlighter = new CSharpDocumentHighlighter(textEditor);
        const string text = "var dict = new Dictionary<string, List<int>>() { };";
        textEditor.AppendText(text);

        highlighter.ApplyHighlight(text);

        DocumentHighlighterTestHelper.AssertTextPreserved(textEditor, text);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "<", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ">", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "<", ScopeType.Brackets, 1);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ">", ScopeType.Brackets, 1);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "(", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, ")", ScopeType.Brackets, 0);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "{", ScopeType.Brackets);
        DocumentHighlighterTestHelper.AssertScopeColor(textEditor, text, "}", ScopeType.Brackets);
    }
}
