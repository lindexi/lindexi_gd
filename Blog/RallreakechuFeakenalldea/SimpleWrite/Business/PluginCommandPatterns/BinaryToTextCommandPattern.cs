using System;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class BinaryToTextCommandPattern(PluginCommandPatternProvider pluginCommandPatternProvider) : ICommandPattern
{
    private static readonly UTF8Encoding Utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        return ValueTask.FromResult(pluginCommandPatternProvider.CanShowSidebarConversation() && TryDecodeBinaryText(text, out _));
    }

    public string Title => "二进制转文本";

    public Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        if (!TryDecodeBinaryText(text, out string result))
        {
            return Task.CompletedTask;
        }

        return pluginCommandPatternProvider.ShowSidebarConversationAsync(
            $"""
             请将以下二进制内容转换为文本：
             {text}
             """,
            $"""
             结果：
             {result}
             """);
    }

    private static bool TryDecodeBinaryText(string text, out string result)
    {
        result = string.Empty;

        string normalizedText = RemoveWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalizedText) || normalizedText.Length % 8 != 0)
        {
            return false;
        }

        byte[] byteArray = new byte[normalizedText.Length / 8];
        for (int i = 0; i < normalizedText.Length; i += 8)
        {
            ReadOnlySpan<char> byteSpan = normalizedText.AsSpan(i, 8);
            for (int j = 0; j < byteSpan.Length; j++)
            {
                if (byteSpan[j] is not ('0' or '1'))
                {
                    return false;
                }
            }

            byteArray[i / 8] = Convert.ToByte(byteSpan.ToString(), 2);
        }

        try
        {
            result = Utf8Encoding.GetString(byteArray);
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
