using System;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class TextToBase64CommandPattern(PluginCommandPatternProvider pluginCommandPatternProvider) : ICommandPattern
{
    private static readonly UTF8Encoding Utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ValueTask.FromResult(pluginCommandPatternProvider.CanShowSidebarConversation() && !string.IsNullOrWhiteSpace(text));
    }

    public string Title => "文本转 Base64";

    public Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        string result = Convert.ToBase64String(Utf8Encoding.GetBytes(text));
        return pluginCommandPatternProvider.ShowSidebarConversationAsync(
            $"""
             请将以下内容转换为 Base64：
             {text}
             """,
            $"""
             结果：
             {result}
             """);
    }
}
