using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class OpenUrlCommandPattern : ICommandPattern
{
    public int Priority => 10;
    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        return ValueTask.FromResult(TryGetUrl(text, out _));
    }

    public string Title => "打开链接";

    public Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        if (!TryGetUrl(text, out string url))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true,
        });

        return Task.CompletedTask;
    }

    public static bool TryGetUrl(string text, out string url)
    {
        url = string.Empty;

        if (!TryNormalizeCandidate(text, out string normalizedText))
        {
            return false;
        }

        if (TryUnwrapMarkdownLink(normalizedText, out string markdownUrl))
        {
            normalizedText = markdownUrl;
        }

        if (normalizedText.StartsWith('<') && normalizedText.EndsWith('>') && normalizedText.Length > 2)
        {
            normalizedText = normalizedText[1..^1].Trim();
        }

        if (!Uri.TryCreate(normalizedText, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        url = uri.AbsoluteUri;
        return true;
    }

    private static bool TryNormalizeCandidate(string text, out string normalizedText)
    {
        normalizedText = text.Trim();
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            normalizedText = string.Empty;
            return false;
        }

        if (normalizedText.Length >= 2
            && normalizedText.StartsWith('`')
            && normalizedText.EndsWith('`')
            && normalizedText.IndexOf('`', 1) == normalizedText.Length - 1)
        {
            normalizedText = normalizedText[1..^1].Trim();
        }

        return !string.IsNullOrWhiteSpace(normalizedText);
    }

    private static bool TryUnwrapMarkdownLink(string text, out string url)
    {
        url = string.Empty;

        int openBracketIndex = text.IndexOf('[');
        int closeBracketIndex = text.IndexOf(']');
        int openParenthesisIndex = text.IndexOf('(');
        int closeParenthesisIndex = text.LastIndexOf(')');

        if (openBracketIndex != 0 || closeBracketIndex <= openBracketIndex || openParenthesisIndex != closeBracketIndex + 1 || closeParenthesisIndex != text.Length - 1)
        {
            return false;
        }

        url = text[(openParenthesisIndex + 1)..closeParenthesisIndex].Trim();
        return !string.IsNullOrWhiteSpace(url);
    }
}