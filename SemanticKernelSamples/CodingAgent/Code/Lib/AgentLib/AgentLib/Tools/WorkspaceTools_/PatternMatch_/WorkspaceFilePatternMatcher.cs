using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentLib.Tools;

/// <summary>
/// 负责对单个文件进行文本或正则表达式匹配，返回命中行的匹配结果（含截断后的上下文）。
/// </summary>
public sealed class WorkspaceFilePatternMatcher
{
    private readonly int _maxLineContextChars;
    private readonly int _maxLineHitsPerFile;
    private readonly string _query;
    private readonly Regex? _regex;
    private readonly int _halfContext;

    /// <summary>
    /// 初始化 <see cref="WorkspaceFilePatternMatcher"/> 的新实例。
    /// </summary>
    /// <param name="maxLineContextChars">每行上下文最大字符数（前后各半）。</param>
    /// <param name="maxLineHitsPerFile">每文件最多返回的命中行数。</param>
    /// <param name="query">要匹配的文本或正则表达式模式。</param>
    /// <param name="regex">已编译的正则表达式；为 null 时使用纯文本匹配。</param>
    public WorkspaceFilePatternMatcher(int maxLineContextChars, int maxLineHitsPerFile, string query, Regex? regex)
    {
        _maxLineContextChars = maxLineContextChars;
        _maxLineHitsPerFile = maxLineHitsPerFile;
        _query = query;
        _regex = regex;
        _halfContext = maxLineContextChars / 2;
    }

    /// <summary>
    /// 对指定文件进行匹配，返回所有命中行信息（含截断后的上下文）。
    /// </summary>
    /// <param name="filePath">文件的完整路径。</param>
    /// <returns>命中行匹配结果。</returns>
    public async Task<WorkspaceFileMatchResults> FindAsync(string filePath)
    {
        List<WorkspaceFileMatchLineResult> results = [];
        int lineNumber = 0;
        bool isTruncated = false;

        try
        {
            using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                lineNumber++;
                if (!TryGetMatch(line, out int matchStartIndex, out int matchLength))
                {
                    continue;
                }

                if (results.Count >= _maxLineHitsPerFile)
                {
                    isTruncated = true;
                    break;
                }

                string truncatedContext = TruncateLineWithContext(line, matchStartIndex, matchLength);
                results.Add(new WorkspaceFileMatchLineResult(lineNumber, line, matchStartIndex, truncatedContext));
            }
        }
        catch (IOException)
        {
            // 文件读取失败，返回空结果
        }
        catch (UnauthorizedAccessException)
        {
            // 无权限访问，返回空结果
        }

        return new WorkspaceFileMatchResults(results, isTruncated);
    }

    private bool TryGetMatch(string line, out int matchStartIndex, out int matchLength)
    {
        if (_regex is not null)
        {
            Match match = _regex.Match(line);
            if (match.Success)
            {
                matchStartIndex = match.Index;
                matchLength = match.Length;
                return true;
            }

            matchStartIndex = 0;
            matchLength = 0;
            return false;
        }

        matchStartIndex = line.IndexOf(_query, StringComparison.OrdinalIgnoreCase);
        if (matchStartIndex >= 0)
        {
            matchLength = _query.Length;
            return true;
        }

        matchLength = 0;
        return false;
    }

    /// <summary>
    /// 将匹配行截断为前后各约 <see cref="_halfContext"/> 字符的上下文，用 … 标记截断位置。
    /// </summary>
    private string TruncateLineWithContext(string line, int matchStartIndex, int matchLength)
    {
        int lineLength = line.Length;
        int contextStart = Math.Max(0, matchStartIndex - _halfContext);
        int contextEnd = Math.Min(lineLength, matchStartIndex + matchLength + _halfContext);

        var builder = new StringBuilder(_maxLineContextChars + 8);
        if (contextStart > 0)
        {
            builder.Append('…');
        }

        builder.Append(line.AsSpan(contextStart, contextEnd - contextStart));

        if (contextEnd < lineLength)
        {
            builder.Append('…');
        }

        return builder.ToString();
    }
}