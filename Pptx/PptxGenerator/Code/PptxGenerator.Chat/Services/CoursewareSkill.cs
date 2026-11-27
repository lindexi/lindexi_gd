using System.IO;
using System.Text.Json;
using OpenAI.Responses;

namespace PptxGenerator.Chat.Services;

internal sealed class CoursewareSkill
{
    internal const string ToolName = "ReadCoursewareSkill";
    private const string EntryFile = "SKILL.md";
    private readonly string _directory;

    internal CoursewareSkill(string directory) => _directory = directory;

    internal async Task<string> RegisterAsync(Dictionary<string, ChatTool> tools, string label, string readFailed,
        CancellationToken token)
    {
        string description = await ReadDescriptionAsync(token).ConfigureAwait(false);
        var definition = ResponseTool.CreateFunctionTool(ToolName, BinaryData.FromString(
            "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\",\"description\":\"SKILL.md 或入口文档中的 references/ 相对路径\"}},\"required\":[\"path\"]}"),
            false, "读取课件技能入口或其参考文档，不依赖工作路径和 MCP。");
        tools.Add(ToolName, new ChatTool(definition, label, false,
            (call, ct) => ReadDocumentAsync(call, readFailed, ct)));
        return $"可用技能：courseware-slideml。{description}\n编写、修改或排查 SlideML 时，先调用 {ToolName} 读取 SKILL.md，再按任务需要读取相关参考文档。普通聊天无需读取技能。";
    }

    private async Task<string> ReadDescriptionAsync(CancellationToken token)
    {
        using var reader = File.OpenText(Path.Combine(_directory, EntryFile));
        if (await reader.ReadLineAsync().WaitAsync(token).ConfigureAwait(false) == "---")
        {
            while (await reader.ReadLineAsync().WaitAsync(token).ConfigureAwait(false) is { } line && line != "---")
            {
                const string key = "description:";
                if (line.StartsWith(key, StringComparison.Ordinal)) return line[key.Length..].Trim();
            }
        }
        return string.Empty;
    }

    private async Task<ToolResult> ReadDocumentAsync(FunctionCallResponseItem call, string readFailed, CancellationToken token)
    {
        try
        {
            using var arguments = JsonDocument.Parse(call.FunctionArguments.ToMemory());
            if (arguments.RootElement.ValueKind != JsonValueKind.Object ||
                !arguments.RootElement.TryGetProperty("path", out var value) || value.ValueKind != JsonValueKind.String ||
                value.GetString() is not { } path || !IsSkillDocument(path))
                return Failure();

            var content = await File.ReadAllTextAsync(Path.Combine(_directory, path), token).ConfigureAwait(false);
            return new ToolResult(ResponseItem.CreateFunctionCallOutputItem(call.CallId, content));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or JsonException)
        {
            ChatDiagnostics.Failure("ReadCoursewareSkill", exception, "Skill", "courseware-slideml");
            return Failure();
        }

        ToolResult Failure() => new(ResponseItem.CreateFunctionCallOutputItem(call.CallId, readFailed), true);
    }

    private static bool IsSkillDocument(string path)
    {
        var parts = path.Split('/');
        return path == EntryFile || (parts.Length == 2 && parts[0] == "references" &&
            parts[1].EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
            parts[1].IndexOfAny(Path.GetInvalidFileNameChars()) < 0);
    }
}
