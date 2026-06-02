using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentLib.Tools;

/// <summary>
/// 辅助结构体，负责对单个文件进行文本或正则表达式匹配，返回命中行号列表。
/// </summary>
public readonly struct WorkspaceFilePatternMatcher
{
    /// <summary>
    /// 对指定文件进行匹配，返回所有命中行号（1-based）。
    /// </summary>
    /// <param name="filePath">文件的完整路径。</param>
    /// <param name="query">要匹配的文本或正则表达式模式。</param>
    /// <param name="regex">已编译的正则表达式；为 null 时使用纯文本匹配。</param>
    /// <returns>命中行号列表。</returns>
    public static async Task<List<int>> FindAsync(string filePath, string query, Regex? regex)
    {
        List<int> lineNumbers = [];
        int lineNumber = 0;

        try
        {
            using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                lineNumber++;
                if (!IsMatch(line, query, regex))
                {
                    continue;
                }

                lineNumbers.Add(lineNumber);
            }
        }
        catch (IOException)
        {
            // 文件读取失败，返回空列表
        }
        catch (UnauthorizedAccessException)
        {
            // 无权限访问，返回空列表
        }

        return lineNumbers;
    }

    private static bool IsMatch(string line, string query, Regex? regex)
    {
        if (regex is not null)
        {
            return regex.IsMatch(line);
        }

        return line.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}