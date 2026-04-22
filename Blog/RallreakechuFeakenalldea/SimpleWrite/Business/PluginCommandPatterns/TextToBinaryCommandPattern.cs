using System;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class TextToBinaryCommandPattern(PluginCommandPatternProvider pluginCommandPatternProvider) : ICommandPattern
{
    private static readonly UTF8Encoding Utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ValueTask.FromResult(pluginCommandPatternProvider.CanShowSidebarConversation() && !string.IsNullOrWhiteSpace(text));
    }

    public string Title => "文本转二进制";

    public Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        byte[] byteArray = Utf8Encoding.GetBytes(text);
        var stringBuilder = new StringBuilder(byteArray.Length * 9);
        for (int i = 0; i < byteArray.Length; i++)
        {
            if (i > 0)
            {
                stringBuilder.Append(' ');
            }

            stringBuilder.Append(Convert.ToString(byteArray[i], 2).PadLeft(8, '0'));
        }

        return pluginCommandPatternProvider.ShowSidebarConversationAsync(
            $"""
             请将以下文本转换为二进制（UTF-8）：
             {text}
             """,
            $"""
             结果：
             {stringBuilder}
             """);
    }
}
