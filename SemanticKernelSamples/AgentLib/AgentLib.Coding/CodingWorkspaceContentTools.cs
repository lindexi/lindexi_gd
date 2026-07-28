using System.ComponentModel;

using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CodingWorkspaceContentTools
{
    private readonly string _workspacePath;

    internal CodingWorkspaceContentTools(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("代码工作区路径不能为空。", nameof(workspacePath));
        }

        _workspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(_workspacePath))
        {
            throw new DirectoryNotFoundException("指定的代码工作区不存在。");
        }
    }

    internal IReadOnlyList<AITool> AsAITools() =>
    [
        AIFunctionFactory.Create(LoadImageAsync, "load_image")
    ];

    [Description("从代码工作区内的图片文件加载多模态图片内容。")]
    internal async Task<DataContent> LoadImageAsync(
        [Description("图片文件路径。可以传绝对路径；相对路径则相对于代码工作区。")]
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("图片文件路径不能为空。", nameof(filePath));
        }

        string fullPath = Path.GetFullPath(Path.IsPathRooted(filePath)
            ? filePath
            : Path.Join(_workspacePath, filePath));
        EnsurePathInsideWorkspace(fullPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("指定的图片文件不存在。", fullPath);
        }

        DataContent content = await DataContent.LoadFromAsync(
            fullPath,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (content.MediaType is null || !content.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("指定文件不是受支持的图片文件。");
        }

        return content;
    }

    private void EnsurePathInsideWorkspace(string path)
    {
        string relativePath = Path.GetRelativePath(_workspacePath, path);
        if (Path.IsPathRooted(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("图片文件必须位于工作区内。");
        }
    }
}
