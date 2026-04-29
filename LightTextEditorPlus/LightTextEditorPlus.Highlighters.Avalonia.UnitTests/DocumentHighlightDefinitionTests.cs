using LightTextEditorPlus.Highlighters;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class DocumentHighlightDefinitionTests
{
    [Fact]
    public void CreateOther_ValidLanguageId_CreatesInstanceWithCorrectCategoryAndLanguageId()
    {
        // Arrange
        const string languageId = "javascript";

        // Act
        var result = DocumentHighlightDefinition.CreateOther(languageId);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal(languageId, result.LanguageId);
    }

    [Fact]
    public void CreateOther_NullLanguageId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DocumentHighlightDefinition.CreateOther(null!));
        Assert.Equal("languageId", exception.ParamName);
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void CreateOther_EmptyStringLanguageId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DocumentHighlightDefinition.CreateOther(string.Empty));
        Assert.Equal("languageId", exception.ParamName);
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void CreateOther_WhitespaceLanguageId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DocumentHighlightDefinition.CreateOther("   "));
        Assert.Equal("languageId", exception.ParamName);
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void CreateOther_TabWhitespaceLanguageId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DocumentHighlightDefinition.CreateOther("\t"));
        Assert.Equal("languageId", exception.ParamName);
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void CreateOther_NewlineWhitespaceLanguageId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => DocumentHighlightDefinition.CreateOther("\n"));
        Assert.Equal("languageId", exception.ParamName);
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Theory]
    [InlineData("python")]
    [InlineData("java")]
    [InlineData("go")]
    [InlineData("rust")]
    [InlineData("typescript")]
    [InlineData("PHP")]
    [InlineData("ruby")]
    [InlineData("kotlin")]
    [InlineData("swift")]
    [InlineData("cpp")]
    public void CreateOther_VariousValidLanguageIds_CreatesInstanceWithCorrectProperties(string languageId)
    {
        // Arrange & Act
        var result = DocumentHighlightDefinition.CreateOther(languageId);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal(languageId, result.LanguageId);
    }

    [Fact]
    public void CreateOther_LanguageIdWithSpecialCharacters_CreatesInstanceSuccessfully()
    {
        // Arrange
        const string languageId = "custom-lang_v1.0";

        // Act
        var result = DocumentHighlightDefinition.CreateOther(languageId);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal(languageId, result.LanguageId);
    }

    [Fact]
    public void CreateOther_LanguageIdWithNumbers_CreatesInstanceSuccessfully()
    {
        // Arrange
        const string languageId = "f#";

        // Act
        var result = DocumentHighlightDefinition.CreateOther(languageId);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal(languageId, result.LanguageId);
    }

    [Fact]
    public void CreateOther_LongLanguageId_CreatesInstanceSuccessfully()
    {
        // Arrange
        const string languageId = "very-long-custom-language-identifier-name";

        // Act
        var result = DocumentHighlightDefinition.CreateOther(languageId);

        // Assert
        Assert.Equal(DocumentHighlightCategory.Other, result.Category);
        Assert.Equal(languageId, result.LanguageId);
    }
}
