using System;
using System.Text.Json;

using Microsoft.CodeAnalysis.Text;

namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 为 JSON 文本提供基于解析结果的高亮。
/// </summary>
internal sealed class JsonCodeHighlighter
{
    private static readonly JsonReaderOptions JsonReaderOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// 尝试按 JSON 语义应用高亮。
    /// </summary>
    /// <param name="context">代码内容与着色输出上下文。</param>
    /// <returns>成功解析并完成高亮时返回 <see langword="true"/>。</returns>
    public bool TryApplyHighlight(in HighlightCodeContext context)
    {
        string code = context.PlainCode;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(code);
            var reader = new Utf8JsonReader(utf8Bytes, JsonReaderOptions);

            while (reader.Read())
            {
                HighlightToken(code, in reader, context.ColorCode);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void HighlightToken(string code, in Utf8JsonReader reader, IColorCode colorCode)
    {
        var scopeType = reader.TokenType switch
        {
            JsonTokenType.PropertyName => ScopeType.ClassMember,
            JsonTokenType.String => ScopeType.String,
            JsonTokenType.Number => ScopeType.Number,
            JsonTokenType.True or JsonTokenType.False or JsonTokenType.Null => ScopeType.DeclarationTypeSyntax,
            _ => (ScopeType?) null
        };

        if (scopeType is null)
        {
            return;
        }

        if (!TryGetTokenSpan(code, in reader, out var span))
        {
            return;
        }

        colorCode.FillCodeColor(span, scopeType.Value);
    }

    private static bool TryGetTokenSpan(string code, in Utf8JsonReader reader, out TextSpan span)
    {
        int start = checked((int) reader.TokenStartIndex);
        if ((uint) start >= (uint) code.Length)
        {
            span = default;
            return false;
        }

        int length = reader.TokenType switch
        {
            JsonTokenType.PropertyName or JsonTokenType.String => GetQuotedTokenLength(code, start),
            JsonTokenType.Number => reader.ValueSpan.Length,
            JsonTokenType.True => 4,
            JsonTokenType.False => 5,
            JsonTokenType.Null => 4,
            _ => 0
        };

        if (length <= 0 || start + length > code.Length)
        {
            span = default;
            return false;
        }

        span = new TextSpan(start, length);
        return true;
    }

    private static int GetQuotedTokenLength(string code, int start)
    {
        if (code[start] != '"')
        {
            return 0;
        }

        for (int index = start + 1; index < code.Length; index++)
        {
            if (code[index] == '\\')
            {
                index++;
                continue;
            }

            if (code[index] == '"')
            {
                return index - start + 1;
            }
        }

        return 0;
    }
}
