using LightTextEditorPlus.Highlighters;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class DocumentHighlighterSelectorTests
{
    #region GetDocumentHighlightDefinition(FileInfo?)

    [Fact]
    public void GetDocumentHighlightDefinition_NullFileInfo_ReturnsMarkdown()
    {
        // Arrange
        FileInfo? fileInfo = null;

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(fileInfo);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FileInfoWithCsExtension_ReturnsCSharp()
    {
        // Arrange
        var fileInfo = new FileInfo("test.cs");

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(fileInfo);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.CSharp, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FileInfoWithMdExtension_ReturnsMarkdown()
    {
        // Arrange
        var fileInfo = new FileInfo("readme.md");

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(fileInfo);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FileInfoWithJavaExtension_ReturnsOtherWithJavaLanguageId()
    {
        // Arrange
        var fileInfo = new FileInfo("program.java");

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(fileInfo);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("java", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FileInfoWithUnknownExtension_ReturnsMarkdown()
    {
        // Arrange
        var fileInfo = new FileInfo("document.unknown");

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(fileInfo);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FileInfoWithNoExtension_ReturnsMarkdown()
    {
        // Arrange
        var fileInfo = new FileInfo("README");

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(fileInfo);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    #endregion

    #region GetDocumentHighlightDefinition(string?)

    [Fact]
    public void GetDocumentHighlightDefinition_NullExtension_ReturnsMarkdown()
    {
        // Arrange
        string? extension = null;

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_EmptyExtension_ReturnsMarkdown()
    {
        // Arrange
        var extension = string.Empty;

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_WhitespaceExtension_ReturnsMarkdown()
    {
        // Arrange
        var extension = "   ";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionWithDotCs_ReturnsCSharp()
    {
        // Arrange
        var extension = ".cs";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.CSharp, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionWithoutDotCs_ReturnsCSharp()
    {
        // Arrange
        var extension = "test.cs";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.CSharp, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionCsx_ReturnsCSharp()
    {
        // Arrange
        var extension = ".csx";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.CSharp, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionMd_ReturnsMarkdown()
    {
        // Arrange
        var extension = ".md";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionMarkdown_ReturnsMarkdown()
    {
        // Arrange
        var extension = ".markdown";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionMdown_ReturnsMarkdown()
    {
        // Arrange
        var extension = ".mdown";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionJava_ReturnsOtherWithJavaLanguageId()
    {
        // Arrange
        var extension = ".java";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("java", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionJs_ReturnsOtherWithJavaScriptLanguageId()
    {
        // Arrange
        var extension = ".js";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("javascript", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionPy_ReturnsOtherWithPythonLanguageId()
    {
        // Arrange
        var extension = ".py";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("python", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionXml_ReturnsOtherWithXmlLanguageId()
    {
        // Arrange
        var extension = ".xml";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("xml", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionXaml_ReturnsOtherWithXmlLanguageId()
    {
        // Arrange
        var extension = ".xaml";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("xml", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionHtml_ReturnsOtherWithHtmlLanguageId()
    {
        // Arrange
        var extension = ".html";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("html", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionCpp_ReturnsOtherWithCppLanguageId()
    {
        // Arrange
        var extension = ".cpp";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("cpp", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionTs_ReturnsOtherWithTypeScriptLanguageId()
    {
        // Arrange
        var extension = ".ts";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("typescript", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionSql_ReturnsOtherWithSqlLanguageId()
    {
        // Arrange
        var extension = ".sql";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("sql", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionJson_ReturnsOtherWithJsonLanguageId()
    {
        // Arrange
        var extension = ".json";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("json", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionVb_ReturnsOtherWithVbDotNetLanguageId()
    {
        // Arrange
        var extension = ".vb";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("vb.net", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionFs_ReturnsOtherWithFSharpLanguageId()
    {
        // Arrange
        var extension = ".fs";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("f#", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionPhp_ReturnsOtherWithPhpLanguageId()
    {
        // Arrange
        var extension = ".php";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("php", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_UnknownExtension_ReturnsMarkdown()
    {
        // Arrange
        var extension = ".unknown";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FilenameWithoutDotAndNoDot_ReturnsMarkdown()
    {
        // Arrange
        var extension = "README";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionCaseInsensitive_ReturnsCSharp()
    {
        // Arrange
        var extension = ".CS";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.CSharp, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionMixedCase_ReturnsMarkdown()
    {
        // Arrange
        var extension = ".Md";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.Markdown, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_FullPathWithCsExtension_ReturnsCSharp()
    {
        // Arrange
        var extension = "C:\\path\\to\\file.cs";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightDefinition.CSharp, result);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_RelativePathWithJavaExtension_ReturnsOtherWithJavaLanguageId()
    {
        // Arrange
        var extension = "src/main/Program.java";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("java", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionAxaml_ReturnsOtherWithXmlLanguageId()
    {
        // Arrange
        var extension = ".axaml";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("xml", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionCshtml_ReturnsOtherWithAspxCsLanguageId()
    {
        // Arrange
        var extension = ".cshtml";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("aspx(c#)", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionPs1_ReturnsOtherWithPowerShellLanguageId()
    {
        // Arrange
        var extension = ".ps1";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("powershell", result.LanguageId);
    }

    [Fact]
    public void GetDocumentHighlightDefinition_ExtensionCss_ReturnsOtherWithCssLanguageId()
    {
        // Arrange
        var extension = ".css";

        // Act
        var result = DocumentHighlighterSelector.GetDocumentHighlightDefinition(extension);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal("css", result.LanguageId);
    }

    #endregion
}
