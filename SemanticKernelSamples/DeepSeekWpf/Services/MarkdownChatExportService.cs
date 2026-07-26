using System.IO;
using System.Text;
using DeepSeekWpf.Models;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Services;

public sealed class MarkdownChatExportService : IChatExportService
{
    public async Task ExportMarkdownAsync(
        ChatSession session,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var builder = new StringBuilder()
            .Append("# ").AppendLine(session.Title)
            .AppendLine()
            .Append("- 创建时间：").AppendLine(session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            .Append("- 更新时间：").AppendLine(session.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
            .AppendLine();

        foreach (var message in session.Messages)
        {
            builder.Append("## ")
                .Append(GetRoleName(message.Role))
                .Append(" · ")
                .AppendLine(message.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                .AppendLine();

            if (!string.IsNullOrWhiteSpace(message.ThoughtContent))
            {
                builder.AppendLine("<details>")
                    .AppendLine("<summary>思考过程</summary>")
                    .AppendLine()
                    .AppendLine(message.ThoughtContent)
                    .AppendLine()
                    .AppendLine("</details>")
                    .AppendLine();
            }

            builder.AppendLine(message.Content).AppendLine();
        }

        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
        var temporaryPath = Path.Combine(
            Path.GetDirectoryName(fullPath) ?? ".",
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(temporaryPath, builder.ToString(), new UTF8Encoding(false), cancellationToken)
                .ConfigureAwait(false);
            File.Move(temporaryPath, fullPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string GetRoleName(ChatRole role)
    {
        if (role == ChatRole.User)
        {
            return "用户";
        }

        return role == ChatRole.Assistant ? "助手" : role.ToString();
    }
}
