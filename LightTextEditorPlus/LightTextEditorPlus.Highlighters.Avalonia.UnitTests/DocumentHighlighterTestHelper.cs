using ColorCode.Common;
using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Highlighters;
using LightTextEditorPlus.Highlighters.CodeHighlighters;

namespace LightTextEditorPlus.Highlighters.Avalonia.UnitTests;

internal static class DocumentHighlighterTestHelper
{
    internal static string GetEditorText(TextEditor textEditor)
    {
        var allSelection = textEditor.GetAllDocumentSelection();
        return textEditor.TextEditorCore.GetText(in allSelection);
    }

    internal static void AssertTextPreserved(TextEditor textEditor, string expectedText)
    {
        Assert.Equal(expectedText, GetEditorText(textEditor));
    }

    internal static void AssertScopeColor(TextEditor textEditor, string text, string token, ScopeType scope, int occurrence = 0)
    {
        var start = GetOccurrenceStart(text, token, occurrence);
        AssertScopeColor(textEditor, start, token.Length, scope);
    }

    internal static void AssertScopeColor(TextEditor textEditor, int start, int length, ScopeType scope)
    {
        var expectedColor = new ColorCodeStyleManager(textEditor).GetRunProperty(scope).Foreground.AsSolidColor();
        foreach (var runProperty in GetCharacterRunProperties(textEditor, start, length))
        {
            Assert.Equal(expectedColor, runProperty.Foreground.AsSolidColor());
        }
    }

    internal static void AssertScopeColor(TextEditor textEditor, string text, string token, params ScopeType[] scopes)
    {
        AssertScopeColor(textEditor, text, token, 0, scopes);
    }

    internal static void AssertScopeColor(TextEditor textEditor, string text, string token, int occurrence, params ScopeType[] scopes)
    {
        var start = GetOccurrenceStart(text, token, occurrence);
        AssertScopeColor(textEditor, start, token.Length, scopes);
    }

    internal static void AssertScopeColor(TextEditor textEditor, int start, int length, params ScopeType[] scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        Assert.NotEmpty(scopes);

        var styleManager = new ColorCodeStyleManager(textEditor);
        var expectedColors = scopes.Select(scope => styleManager.GetRunProperty(scope).Foreground.AsSolidColor()).ToHashSet();
        foreach (var runProperty in GetCharacterRunProperties(textEditor, start, length))
        {
            Assert.Contains(runProperty.Foreground.AsSolidColor(), expectedColors);
        }
    }

    internal static void AssertTokenUsesNonPlainTextColor(TextEditor textEditor, string text, string token, int occurrence = 0)
    {
        var start = GetOccurrenceStart(text, token, occurrence);
        AssertUsesNonPlainTextColor(textEditor, start, token.Length);
    }

    internal static void AssertUsesNonPlainTextColor(TextEditor textEditor, int start, int length)
    {
        var plainTextColor = new ColorCodeStyleManager(textEditor).GetRunProperty(ScopeType.PlainText).Foreground.AsSolidColor();
        Assert.Contains(GetCharacterRunProperties(textEditor, start, length), runProperty => runProperty.Foreground.AsSolidColor() != plainTextColor);
    }

    internal static void AssertDocumentContainsNonPlainTextColor(TextEditor textEditor)
    {
        var allSelection = textEditor.GetAllDocumentSelection();
        var runPropertyList = textEditor.GetRunPropertyRange(allSelection).ToList();
        Assert.NotEmpty(runPropertyList);

        var plainTextColor = new ColorCodeStyleManager(textEditor).GetRunProperty(ScopeType.PlainText).Foreground.AsSolidColor();
        Assert.Contains(runPropertyList, runProperty => runProperty.Foreground.AsSolidColor() != plainTextColor);
    }

    internal static void AssertSameForegroundColors(TextEditor expectedEditor, int expectedStart, TextEditor actualEditor, int actualStart, int length)
    {
        Assert.True(length >= 0);

        var expectedRunPropertyList = GetCharacterRunProperties(expectedEditor, expectedStart, length).ToList();
        var actualRunPropertyList = GetCharacterRunProperties(actualEditor, actualStart, length).ToList();

        Assert.Equal(expectedRunPropertyList.Count, actualRunPropertyList.Count);

        for (var index = 0; index < expectedRunPropertyList.Count; index++)
        {
            Assert.Equal(expectedRunPropertyList[index].Foreground.AsSolidColor(), actualRunPropertyList[index].Foreground.AsSolidColor());
        }
    }

    internal static void AssertPlainTextColor(TextEditor textEditor, string text)
    {
        AssertScopeColor(textEditor, 0, text.Length, ScopeType.PlainText);
    }

    private static IEnumerable<dynamic> GetCharacterRunProperties(TextEditor textEditor, int start, int length)
    {
        Assert.True(length >= 0);

        if (length == 0)
        {
            yield break;
        }

        for (var index = 0; index < length; index++)
        {
            var selection = new Selection(new CaretOffset(start + index), 1);
            var runPropertyList = textEditor.GetRunPropertyRange(selection).ToList();
            Assert.NotEmpty(runPropertyList);

            yield return runPropertyList[^1];
        }
    }

    internal static int GetOccurrenceStart(string text, string token, int occurrence)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(token);
        Assert.False(string.IsNullOrEmpty(token));
        Assert.True(occurrence >= 0);

        var searchStart = 0;
        for (var index = 0; index <= occurrence; index++)
        {
            var foundIndex = text.IndexOf(token, searchStart, StringComparison.Ordinal);
            Assert.True(foundIndex >= 0, $"Token '{token}' occurrence {occurrence} was not found in '{text}'.");

            if (index == occurrence)
            {
                return foundIndex;
            }

            searchStart = foundIndex + token.Length;
        }

        throw new InvalidOperationException("Unreachable.");
    }
}
