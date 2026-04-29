using LightTextEditorPlus.Highlighters.CodeHighlighters;
using Microsoft.CodeAnalysis.Text;
using Moq;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests.CodeHighlighters;

public class CsharpCodeHighlighterTests
{
    [Fact]
    public void ApplyHighlight_EmptyCode_FillsPlainText()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        mockColorCode.Verify(x => x.FillCodeColor(It.IsAny<TextSpan>(), It.IsAny<ScopeType>()), Times.Never);
    }

    [Fact]
    public void ApplyHighlight_SimpleKeyword_HighlightsKeyword()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_ClassDeclaration_HighlightsKeywordAndClassName()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class MyClass { }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "class" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // "MyClass" class name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 6 && span.Length == 7),
            ScopeType.ClassName), Times.Once);

        // Brackets
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 14 && span.Length == 1),
            ScopeType.Brackets), Times.Once);

        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 16 && span.Length == 1),
            ScopeType.Brackets), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_StringLiteral_HighlightsString()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var s = \"hello\";";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "var" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "hello" string literal
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 7),
            ScopeType.String), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_NumericLiteral_HighlightsNumber()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var n = 42;";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "var" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "42" number
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 2),
            ScopeType.Number), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_SingleLineComment_HighlightsComment()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "// This is a comment";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 20),
            ScopeType.Comment), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_MultiLineComment_HighlightsComment()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "/* comment */";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 13),
            ScopeType.Comment), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_MethodDeclaration_HighlightsKeywordsAndMethodName()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class C { void MyMethod() { } }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "class" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // "void" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 10 && span.Length == 4),
            ScopeType.Keyword), Times.Once);

        // "MyMethod" method name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 15 && span.Length == 8),
            ScopeType.ClassMember), Times.Once);

        // Parentheses
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 23 && span.Length == 1),
            ScopeType.Brackets), Times.Once);

        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 24 && span.Length == 1),
            ScopeType.Brackets), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_MethodInvocation_HighlightsInvocation()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "Console.WriteLine();";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "WriteLine" invocation
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 9),
            ScopeType.Invocation), Times.Once);

        // Parentheses
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 17 && span.Length == 1),
            ScopeType.Brackets), Times.Once);

        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 18 && span.Length == 1),
            ScopeType.Brackets), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_LocalVariableDeclarationWithVar_HighlightsVarAsKeyword()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var x = 5;";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "var" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "x" variable name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 4 && span.Length == 1),
            ScopeType.Variable), Times.Once);

        // "5" number
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 1),
            ScopeType.Number), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_LocalVariableDeclarationWithExplicitType_HighlightsTypeAsDeclarationTypeSyntax()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "int x = 5;";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "int" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "int" also as DeclarationTypeSyntax
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.DeclarationTypeSyntax), Times.Once);

        // "x" variable name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 4 && span.Length == 1),
            ScopeType.Variable), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_PropertyDeclaration_HighlightsPropertyNameAsClassMember()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "int MyProperty { get; set; }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "int" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "MyProperty" property name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 4 && span.Length == 10),
            ScopeType.ClassMember), Times.Once);

        // "get" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 17 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "set" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 22 && span.Length == 3),
            ScopeType.Keyword), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_ParameterInMethod_HighlightsParameterAsVariable()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class C { void Method(int param) { } }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "class" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // "void" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 10 && span.Length == 4),
            ScopeType.Keyword), Times.Once);

        // "Method" method name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 15 && span.Length == 6),
            ScopeType.ClassMember), Times.Once);

        // "int" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 22 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "param" parameter name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 26 && span.Length == 5),
            ScopeType.Variable), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_ForEachStatement_HighlightsIteratorVariableAsVariable()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "foreach (var item in list) { }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "foreach" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 7),
            ScopeType.Keyword), Times.Once);

        // "item" iterator variable
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 13 && span.Length == 4),
            ScopeType.Variable), Times.Once);

        // "in" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 18 && span.Length == 2),
            ScopeType.Keyword), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_CharacterLiteral_HighlightsAsString()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var c = 'A';";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "var" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // 'A' character literal
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 3),
            ScopeType.String), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_InterpolatedString_HighlightsStringParts()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var s = $\"{name}\";";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "var" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // String parts (interpolated string tokens)
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.String), Times.AtLeastOnce);
    }

    [Fact]
    public void ApplyHighlight_Brackets_HighlightsAllBracketTypes()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "{ [ ( < > ) ] }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // All brackets should be highlighted
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Brackets), Times.Exactly(8));
    }

    [Fact]
    public void ApplyHighlight_FieldDeclaration_HighlightsFieldNameAsClassMember()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class C { int field; }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "class" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // "C" class name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 6 && span.Length == 1),
            ScopeType.ClassName), Times.Once);

        // "int" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 10 && span.Length == 3),
            ScopeType.Keyword), Times.Once);

        // "field" field name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 14 && span.Length == 5),
            ScopeType.ClassMember), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_ConstructorDeclaration_HighlightsConstructorNameAsClassMember()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class MyClass { MyClass() { } }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "class" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // First "MyClass" - class name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 6 && span.Length == 7),
            ScopeType.ClassName), Times.Once);

        // Second "MyClass" - constructor name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 16 && span.Length == 7),
            ScopeType.ClassMember), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_EventDeclaration_HighlightsEventName()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "class C { event EventHandler MyEvent; }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "class" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // "event" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 10 && span.Length == 5),
            ScopeType.Keyword), Times.Once);

        // "MyEvent" event name - currently highlighted as Variable (not ClassMember as might be expected)
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 29 && span.Length == 7),
            ScopeType.Variable), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_ComplexCode_HighlightsAllElements()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = @"// Comment
class MyClass
{
    private int field = 42;
    
    public void Method(string param)
    {
        var local = ""hello"";
        Console.WriteLine(local);
    }
}";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // Comment
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Comment), Times.AtLeastOnce);

        // Keywords (class, private, int, public, void, string, var)
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Keyword), Times.AtLeastOnce);

        // Class name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.ClassName), Times.AtLeastOnce);

        // Class members (field, Method)
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.ClassMember), Times.AtLeastOnce);

        // Variables (param, local)
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Variable), Times.AtLeastOnce);

        // String literal
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.String), Times.AtLeastOnce);

        // Number literal
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Number), Times.AtLeastOnce);

        // Method invocation
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Invocation), Times.AtLeastOnce);

        // Brackets
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Brackets), Times.AtLeastOnce);

        // Plain text
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.PlainText), Times.AtLeastOnce);
    }

    [Fact]
    public void ApplyHighlight_XmlDocComment_HighlightsAsComment()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = @"/// <summary>
/// Documentation
/// </summary>
void Method() { }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // XML doc comments should be highlighted as comments
        mockColorCode.Verify(x => x.FillCodeColor(
            It.IsAny<TextSpan>(),
            ScopeType.Comment), Times.AtLeastOnce);
    }

    [Fact]
    public void ApplyHighlight_FloatingPointNumber_HighlightsAsNumber()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var d = 3.14;";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "3.14" should be highlighted as number
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 4),
            ScopeType.Number), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_HexadecimalNumber_HighlightsAsNumber()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "var h = 0xFF;";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "0xFF" should be highlighted as number
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 8 && span.Length == 4),
            ScopeType.Number), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_InterfaceDeclaration_HighlightsInterfaceNameAsClassName()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "interface IMyInterface { }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "interface" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 9),
            ScopeType.Keyword), Times.Once);

        // "IMyInterface" interface name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 10 && span.Length == 12),
            ScopeType.ClassName), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_StructDeclaration_HighlightsStructNameAsClassName()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "struct MyStruct { }";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "struct" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 6),
            ScopeType.Keyword), Times.Once);

        // "MyStruct" struct name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 7 && span.Length == 8),
            ScopeType.ClassName), Times.Once);
    }

    [Fact]
    public void ApplyHighlight_RecordDeclaration_HighlightsRecordNameAsClassName()
    {
        // Arrange
        var highlighter = new CsharpCodeHighlighter();
        var code = "record MyRecord;";
        var mockColorCode = new Mock<IColorCode>();
        var context = new HighlightCodeContext(code, mockColorCode.Object);

        // Act
        highlighter.ApplyHighlight(context);

        // Assert
        // "record" keyword
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 0 && span.Length == 6),
            ScopeType.Keyword), Times.Once);

        // "MyRecord" record name
        mockColorCode.Verify(x => x.FillCodeColor(
            It.Is<TextSpan>(span => span.Start == 7 && span.Length == 8),
            ScopeType.ClassName), Times.Once);
    }
}
