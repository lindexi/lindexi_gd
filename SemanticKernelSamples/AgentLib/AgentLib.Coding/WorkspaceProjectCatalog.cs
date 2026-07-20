using System.Text.Json;
using System.Xml.Linq;

namespace AgentLib.Coding;

/// <summary>
/// 提供工作区中的解决方案、项目与项目文件目录查询能力。
/// </summary>
public sealed class WorkspaceProjectCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _workspacePath;

    /// <summary>
    /// 使用指定工作区创建项目目录查询器。
    /// </summary>
    /// <param name="workspacePath">工作区根目录。</param>
    public WorkspaceProjectCatalog(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("工作区路径不能为空。", nameof(workspacePath));
        }

        _workspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(_workspacePath))
        {
            throw new DirectoryNotFoundException("指定的工作区不存在。");
        }
    }

    /// <summary>
    /// 返回指定解决方案中的项目；未指定解决方案时自动选择工作区根目录中的唯一解决方案。
    /// </summary>
    /// <param name="solutionPath">解决方案的绝对路径或工作区相对路径。</param>
    /// <returns>包含工作区、解决方案和项目路径的 JSON。</returns>
    public string GetProjectsInSolution(string? solutionPath = null)
    {
        string? resolvedSolution = ResolveSolutionPath(solutionPath);
        IReadOnlyList<string> projects = resolvedSolution is null
            ? Directory.EnumerateFiles(_workspacePath, "*.*proj", SearchOption.AllDirectories)
                .Where(IsSupportedProject)
                .Where(path => !IsIgnoredPath(path))
                .Select(Path.GetFullPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : ParseSolutionProjects(resolvedSolution);

        return JsonSerializer.Serialize(new
        {
            workspace = ".",
            solution = resolvedSolution is null ? null : ToRelativePath(resolvedSolution),
            projects = projects.Select(ToRelativePath)
        }, JsonOptions);
    }

    /// <summary>
    /// 返回指定项目包含的文件。
    /// </summary>
    /// <param name="projectPath">项目的绝对路径或工作区相对路径。</param>
    /// <returns>包含项目路径和文件路径的 JSON。</returns>
    public string GetFilesInProject(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("项目路径不能为空。", nameof(projectPath));
        }

        string fullProjectPath = ResolvePath(projectPath);
        if (!File.Exists(fullProjectPath) || !IsSupportedProject(fullProjectPath))
        {
            throw new FileNotFoundException("找不到受支持的项目文件。");
        }

        string projectDirectory = Path.GetDirectoryName(fullProjectPath)!;
        XDocument project = XDocument.Load(fullProjectPath);
        bool enableDefaultItems = !string.Equals(
            project.Descendants().FirstOrDefault(element => element.Name.LocalName == "EnableDefaultItems")?.Value,
            "false",
            StringComparison.OrdinalIgnoreCase);

        HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);
        if (enableDefaultItems)
        {
            foreach (string file in Directory.EnumerateFiles(projectDirectory, "*", SearchOption.AllDirectories)
                         .Where(path => !IsIgnoredPath(path)))
            {
                files.Add(Path.GetFullPath(file));
            }
        }

        foreach (XElement item in project.Descendants().Where(element =>
                     element.Name.LocalName is "Compile" or "Content" or "None" or "EmbeddedResource"))
        {
            string? include = item.Attribute("Include")?.Value;
            string? remove = item.Attribute("Remove")?.Value;
            if (!string.IsNullOrWhiteSpace(include))
            {
                AddPattern(files, projectDirectory, include);
            }

            if (!string.IsNullOrWhiteSpace(remove))
            {
                RemovePattern(files, projectDirectory, remove);
            }
        }

        return JsonSerializer.Serialize(new
        {
            project = ToRelativePath(fullProjectPath),
            files = files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).Select(ToRelativePath)
        }, JsonOptions);
    }

    private string? ResolveSolutionPath(string? solutionPath)
    {
        if (!string.IsNullOrWhiteSpace(solutionPath))
        {
            string resolved = ResolvePath(solutionPath);
            return File.Exists(resolved)
                ? resolved
                : throw new FileNotFoundException("找不到解决方案文件。");
        }

        string[] solutions = Directory.EnumerateFiles(_workspacePath, "*.sln", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(_workspacePath, "*.slnx", SearchOption.TopDirectoryOnly))
            .ToArray();
        return solutions.Length == 1 ? solutions[0] : null;
    }

    private IReadOnlyList<string> ParseSolutionProjects(string solutionPath) =>
        Path.GetExtension(solutionPath).Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            ? ParseSlnxProjects(solutionPath)
            : ParseSlnProjects(solutionPath);

    private IReadOnlyList<string> ParseSlnxProjects(string solutionPath)
    {
        string solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        XDocument solution = XDocument.Load(solutionPath);
        return solution.Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(Path.Join(solutionDirectory, path!)))
            .Where(IsPathInsideWorkspace)
            .Where(IsSupportedProject)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> ParseSlnProjects(string solutionPath)
    {
        string solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        return File.ReadLines(solutionPath)
            .Where(line => line.StartsWith("Project(", StringComparison.Ordinal))
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 2)
            .Select(parts => parts[1].Trim().Trim('"'))
            .Where(IsSupportedProject)
            .Select(path => Path.GetFullPath(Path.Join(solutionDirectory, path)))
            .Where(IsPathInsideWorkspace)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void AddPattern(HashSet<string> files, string projectDirectory, string pattern)
    {
        if (!ContainsWildcard(pattern))
        {
            string fullPath = Path.GetFullPath(Path.Join(projectDirectory, pattern));
            if (IsPathInsideWorkspace(fullPath) && File.Exists(fullPath))
            {
                files.Add(fullPath);
            }

            return;
        }

        foreach (string file in EnumeratePattern(projectDirectory, pattern))
        {
            files.Add(file);
        }
    }

    private void RemovePattern(HashSet<string> files, string projectDirectory, string pattern)
    {
        if (!ContainsWildcard(pattern))
        {
            string fullPath = Path.GetFullPath(Path.Join(projectDirectory, pattern));
            if (IsPathInsideWorkspace(fullPath))
            {
                files.Remove(fullPath);
            }

            return;
        }

        foreach (string file in EnumeratePattern(projectDirectory, pattern))
        {
            files.Remove(file);
        }
    }

    private IEnumerable<string> EnumeratePattern(string projectDirectory, string pattern)
    {
        string normalized = pattern.Replace('/', Path.DirectorySeparatorChar);
        int wildcardIndex = normalized.IndexOfAny(['*', '?']);
        int separatorIndex = normalized.LastIndexOf(Path.DirectorySeparatorChar, wildcardIndex);
        string relativeRoot = separatorIndex < 0 ? string.Empty : normalized[..separatorIndex];
        string searchPattern = separatorIndex < 0 ? normalized : normalized[(separatorIndex + 1)..];
        string root = Path.GetFullPath(Path.Join(projectDirectory, relativeRoot));
        if (!IsPathInsideWorkspace(root) || !Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
            .Where(IsPathInsideWorkspace)
            .Where(path => !IsIgnoredPath(path))
            .Select(Path.GetFullPath);
    }

    private string ResolvePath(string path)
    {
        string fullPath = Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Join(_workspacePath, path));
        if (!IsPathInsideWorkspace(fullPath))
        {
            throw new UnauthorizedAccessException("目标路径必须位于工作区内。");
        }

        return fullPath;
    }

    private string ToRelativePath(string path) => Path.GetRelativePath(_workspacePath, path)
        .Replace(Path.DirectorySeparatorChar, '/');

    private bool IsPathInsideWorkspace(string path)
    {
        string relativePath = Path.GetRelativePath(_workspacePath, Path.GetFullPath(path));
        return !Path.IsPathRooted(relativePath)
            && !relativePath.Equals("..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static bool ContainsWildcard(string path) => path.IndexOfAny(['*', '?']) >= 0;

    private static bool IsSupportedProject(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredPath(string path)
    {
        string[] segments = Path.GetFullPath(path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => segment is "bin" or "obj" or ".git" or ".vs");
    }
}
