using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IChatExportService
{
    Task ExportMarkdownAsync(ChatSession session, string filePath, CancellationToken cancellationToken = default);
}
