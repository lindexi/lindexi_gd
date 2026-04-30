using System;
using System.Text;
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
            var utf8Bytes = Encoding.UTF8.GetBytes(code);
            var reader = new Utf8JsonReader(utf8Bytes, JsonReaderOptions);

            while (reader.Read())
            {
                HighlightToken(utf8Bytes, in reader, context.ColorCode);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void HighlightToken(byte[] utf8Bytes, in Utf8JsonReader reader, IColorCode colorCode)
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

        if (!TryGetTokenSpan(utf8Bytes, in reader, out var span))
        {
            return;
        }

        colorCode.FillCodeColor(span, scopeType.Value);
    }

    private static bool TryGetTokenSpan(byte[] utf8Bytes, in Utf8JsonReader reader, out TextSpan span)
    {
        int byteStart = checked((int) reader.TokenStartIndex);
        if ((uint) byteStart >= (uint) utf8Bytes.Length)
        {
            span = default;
            return false;
        }

        int byteLength = reader.TokenType switch
        {
            JsonTokenType.PropertyName or JsonTokenType.String => GetQuotedTokenByteLength(utf8Bytes, byteStart),
            JsonTokenType.Number => reader.ValueSpan.Length,
            JsonTokenType.True => 4,
            JsonTokenType.False => 5,
            JsonTokenType.Null => 4,
            _ => 0
        };

        if (byteLength <= 0 || byteStart + byteLength > utf8Bytes.Length)
        {
            span = default;
            return false;
        }

        int charStart = Encoding.UTF8.GetCharCount(utf8Bytes, 0, byteStart);
        int charLength = Encoding.UTF8.GetCharCount(utf8Bytes, byteStart, byteLength);
        span = new TextSpan(charStart, charLength);
        return true;
    }

    private static int GetQuotedTokenByteLength(byte[] utf8Bytes, int byteStart)
    {
        if (utf8Bytes[byteStart] != '"')
        {
            return 0;
        }

        for (int index = byteStart + 1; index < utf8Bytes.Length; index++)
        {
            if (utf8Bytes[index] == '\\')
            {
                index++;
                continue;
            }

            if (utf8Bytes[index] == '"')
            {
                return index - byteStart + 1;
            }
        }

        return 0;
    }
}
