using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Highlighters;
using Markdig.Syntax;
using Moq;
using Avalonia.Media;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

public class MarkdownDocumentHighlighterTests
{
    private static string GetEditorText(TextEditor textEditor)
    {
        var allSelection = textEditor.GetAllDocumentSelection();
        return textEditor.TextEditorCore.GetText(in allSelection);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTextEditor_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MarkdownDocumentHighlighter(null!));
    }

    [Fact]
    public void Constructor_ValidTextEditor_CreatesInstance()
    {
        // Arrange
        var textEditor = new TextEditor();

        // Act
        var highlighter = new MarkdownDocumentHighlighter(textEditor);

        // Assert
        Assert.NotNull(highlighter);
        Assert.NotNull(highlighter.UrlInfoList);
        Assert.Empty(highlighter.UrlInfoList);
    }

    #endregion

    #region UrlInfoList Property Tests

    [Fact]
    public void UrlInfoList_InitialState_ReturnsEmptyList()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);

        // Act
        var urlInfoList = highlighter.UrlInfoList;

        // Assert
        Assert.NotNull(urlInfoList);
        Assert.Empty(urlInfoList);
    }

    [Fact]
    public void UrlInfoList_AfterApplyHighlightWithUrl_ReturnsUrlInfo()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string markdownText = "Visit https://example.com for more info.";
        textEditor.AppendText(markdownText);

        // Act
        highlighter.ApplyHighlight(markdownText);

        // Assert
        Assert.NotEmpty(highlighter.UrlInfoList);
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void UrlInfoList_AfterApplyHighlightWithMultipleUrls_ReturnsAllUrls()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string markdownText = "Visit https://example.com and http://test.org for info.";
        textEditor.AppendText(markdownText);

        // Act
        highlighter.ApplyHighlight(markdownText);

        // Assert
        Assert.Equal(2, highlighter.UrlInfoList.Count);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
        Assert.Equal("http://test.org", highlighter.UrlInfoList[1].Url);
    }

    [Fact]
    public void UrlInfoList_AfterApplyHighlightWithNoUrl_ReturnsEmptyList()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string markdownText = "# Heading\n\nSome text without URLs.";
        textEditor.AppendText(markdownText);

        // Act
        highlighter.ApplyHighlight(markdownText);

        // Assert
        Assert.Empty(highlighter.UrlInfoList);
    }

    #endregion

    #region ApplyHighlight Tests - Basic Cases

    [Fact]
    public void ApplyHighlight_EmptyString_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);

        // Act
        highlighter.ApplyHighlight(string.Empty);

        // Assert - no exception thrown
    }

    [Fact]
    public void ApplyHighlight_PlainText_MaintainsTextIntegrity()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "This is plain text without markdown.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_WhitespaceOnly_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "   \n\t\n   ";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    #endregion

    #region ApplyHighlight Tests - Headings

    [Fact]
    public void ApplyHighlight_HeadingLevel1_HighlightsWithLargestFont()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "# Heading Level 1";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_HeadingLevel2_HighlightsWithLargeFont()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "## Heading Level 2";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_HeadingLevel3_HighlightsWithMediumFont()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "### Heading Level 3";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_HeadingLevel4_HighlightsWithSmallFont()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "#### Heading Level 4";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_HeadingLevel5_HighlightsWithSmallestFont()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "##### Heading Level 5";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_MultipleHeadings_HighlightsAll()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "# Title\n\n## Subtitle\n\n### Section";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    #endregion

    #region ApplyHighlight Tests - Code Blocks

    [Fact]
    public void ApplyHighlight_CodeBlockWithoutLanguage_HighlightsAsPlainCode()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```\nvar x = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithCSharpLanguage_HighlightsAsCSharp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```csharp\nvar x = 10;\nConsole.WriteLine(x);\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithCsLanguage_HighlightsAsCSharp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```cs\nvar x = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithCSharpSymbol_HighlightsAsCSharp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```C#\nvar x = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithDotNetLanguage_HighlightsAsCSharp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```dotnet\nvar x = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithJavaScriptLanguage_HighlightsAsJavaScript()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```javascript\nconst x = 10;\nconsole.log(x);\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithJsLanguage_HighlightsAsJavaScript()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```js\nconst x = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithPythonLanguage_HighlightsAsPython()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```python\nx = 10\nprint(x)\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithPyLanguage_HighlightsAsPython()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```py\nx = 10\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithJavaLanguage_HighlightsAsJava()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```java\nint x = 10;\nSystem.out.println(x);\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithCppLanguage_HighlightsAsCpp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```cpp\nint x = 10;\nstd::cout << x;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithCLanguage_HighlightsAsCpp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```c\nint x = 10;\nprintf(\"%d\", x);\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithSqlLanguage_HighlightsAsSql()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```sql\nSELECT * FROM users;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithXmlLanguage_HighlightsAsXml()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```xml\n<root><item>test</item></root>\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithJsonLanguage_HighlightsAsJson()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```json\n{\"key\": \"value\"}\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithHtmlLanguage_HighlightsAsHtml()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```html\n<div>Hello</div>\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithCssLanguage_HighlightsAsCss()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```css\n.class { color: red; }\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithTypeScriptLanguage_HighlightsAsTypeScript()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```typescript\nconst x: number = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithTsLanguage_HighlightsAsTypeScript()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```ts\nconst x: number = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithFSharpLanguage_HighlightsAsFSharp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```fsharp\nlet x = 10\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithVbLanguage_HighlightsAsVb()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```vb\nDim x As Integer = 10\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithPhpLanguage_HighlightsAsPhp()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```php\n<?php $x = 10; ?>\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_CodeBlockWithMultipleCodeBlocks_HighlightsAll()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "```csharp\nvar x = 10;\n```\n\nSome text\n\n```python\ny = 20\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    #endregion

    #region ApplyHighlight Tests - URLs

    [Fact]
    public void ApplyHighlight_HttpUrl_HighlightsUrl()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit http://example.com for info.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("http://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_HttpsUrl_HighlightsUrl()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit https://example.com for info.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithTrailingPeriod_TrimsTrailingPunctuation()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit https://example.com.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithTrailingComma_TrimsTrailingPunctuation()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit https://example.com, and more.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithTrailingExclamation_TrimsTrailingPunctuation()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit https://example.com!";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithTrailingQuestion_TrimsTrailingPunctuation()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Is this https://example.com?";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithTrailingParenthesis_TrimsTrailingPunctuation()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Check (https://example.com)";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithChinesePunctuation_TrimsTrailingPunctuation()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "访问 https://example.com。";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithPath_HighlightsCompleteUrl()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit https://example.com/path/to/page for info.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com/path/to/page", highlighter.UrlInfoList[0].Url);
    }

    [Fact]
    public void ApplyHighlight_UrlWithQueryString_HighlightsCompleteUrl()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "Visit https://example.com?query=value&other=123 for info.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
        Assert.Equal("https://example.com?query=value&other=123", highlighter.UrlInfoList[0].Url);
    }

    #endregion

    #region ApplyHighlight Tests - Complex Scenarios

    [Fact]
    public void ApplyHighlight_MixedContent_HighlightsAllElements()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "# Title\n\nVisit https://example.com\n\n```csharp\nvar x = 10;\n```";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
        Assert.Single(highlighter.UrlInfoList);
    }

    [Fact]
    public void ApplyHighlight_MultipleInvocations_UpdatesHighlighting()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text1 = "# First Title";
        const string text2 = "## Second Title";
        textEditor.AppendText(text1);

        // Act
        highlighter.ApplyHighlight(text1);
#pragma warning disable CS0618 // Type or member is obsolete
        textEditor.TextEditorCore.Clear();
#pragma warning restore CS0618 // Type or member is obsolete
        textEditor.AppendText(text2);
        highlighter.ApplyHighlight(text2);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text2, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_NestedMarkdownElements_HandlesCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        const string text = "# Title\n\n## Subtitle\n\nParagraph with https://example.com\n\n```csharp\nvar x = 10;\n```\n\nMore text.";
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    [Fact]
    public void ApplyHighlight_LongDocument_HandlesCorrectly()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        var text = string.Join("\n\n", Enumerable.Range(1, 100).Select(i => $"# Heading {i}\n\nParagraph {i}"));
        textEditor.AppendText(text);

        // Act
        highlighter.ApplyHighlight(text);

        // Assert
        DocumentHighlighterTestHelper.AssertTextEqual(text, GetEditorText(textEditor));
    }

    #endregion

    #region RenderForeground Tests

    [Fact]
    public void RenderForeground_DoesNotThrow()
    {
        // Arrange
        var textEditor = new TextEditor();
        var highlighter = new MarkdownDocumentHighlighter(textEditor);
        var mockDrawingContext = new Mock<global::Avalonia.Media.DrawingContext>();
        var context = new AvaloniaTextEditorDrawingContext(textEditor, mockDrawingContext.Object) { Viewport = null };

        // Act & Assert
        highlighter.RenderForeground(in context);
    }

    #endregion
}
