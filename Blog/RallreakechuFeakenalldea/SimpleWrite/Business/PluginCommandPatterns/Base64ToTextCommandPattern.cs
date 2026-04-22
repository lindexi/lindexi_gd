using System;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class Base64ToTextCommandPattern(PluginCommandPatternProvider pluginCommandPatternProvider) : ICommandPattern
{
    private static readonly UTF8Encoding Utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        return ValueTask.FromResult(pluginCommandPatternProvider.CanShowSidebarConversation() && TryDecodeBase64(text, out _));
    }

    public string Title => "Base64 转文本";

    public Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        if (!TryDecodeBase64(text, out string result))
        {
            return Task.CompletedTask;
        }

        return pluginCommandPatternProvider.ShowSidebarConversationAsync(
            $"""
             请将以下 Base64 内容转换为文本：
             {text}
             """,
            $"""
             结果：
             {result}
             """);
    }

    private static bool TryDecodeBase64(string text, out string result)
    {
        result = string.Empty;

        string normalizedText = RemoveWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalizedText) || normalizedText.Length % 4 != 0)
        {
            return false;
        }

        byte[] buffer = new byte[normalizedText.Length];
        if (!Convert.TryFromBase64String(normalizedText, buffer, out int bytesWritten))
        {
            return false;
        }

        try
        {
            result = Utf8Encoding.GetString(buffer, 0, bytesWritten);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string RemoveWhitespace(string text)
    {
        var stringBuilder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString();
    }
}
