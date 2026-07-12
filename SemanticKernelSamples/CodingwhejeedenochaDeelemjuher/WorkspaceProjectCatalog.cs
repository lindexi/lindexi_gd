using System.Text.Json;
using System.Xml.Linq;

namespace CodingwhejeedenochaDeelemjuher;

public sealed class WorkspaceProjectCatalog(string workspacePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string _workspacePath = Path.GetFullPath(workspacePath);

    public string GetProjectsInSolution(string? solutionPath = null)
    {
        string? resolvedSolution = ResolveSolutionPath(solutionPath);
        IReadOnlyList<string> projects = resolvedSolution is null
            ? Directory.EnumerateFiles(_workspacePath, "*.*proj", SearchOption.AllDirectories)
                .Where(IsSupportedProject)
                .Where(path => !IsIgnoredPath(path))
                .Select(Path.GetFullPath)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : ParseSolutionProjects(resolvedSolution);

        return JsonSerializer.Serialize(new
        {
            workspace = _workspacePath,
            solution = resolvedSolution,
            projects = projects.Select(ToRelativePath)
        }, JsonOptions);
    }

    public string GetFilesInProject(string projectPath)
    {
        string fullProjectPath = ResolvePath(projectPath);
        if (!File.Exists(fullProjectPath) || !IsSupportedProject(fullProjectPath))
        {
            throw new FileNotFoundException("找不到受支持的项目文件。", fullProjectPath);
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
            files = files.Order(StringComparer.OrdinalIgnoreCase).Select(ToRelativePath)
        }, JsonOptions);
    }

    private string? ResolveSolutionPath(string? solutionPath)
    {
        if (!string.IsNullOrWhiteSpace(solutionPath))
        {
            string resolved = ResolvePath(solutionPath);
            return File.Exists(resolved)
                ? resolved
                : throw new FileNotFoundException("找不到解决方案文件。", resolved);
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
            .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path!)))
            .Where(IsSupportedProject)
            .Order(StringComparer.OrdinalIgnoreCase)
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
            .Select(path => Path.GetFullPath(Path.Combine(solutionDirectory, path)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void AddPattern(HashSet<string> files, string projectDirectory, string pattern)
    {
        if (!ContainsWildcard(pattern))
        {
            files.Add(Path.GetFullPath(Path.Combine(projectDirectory, pattern)));
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
            files.Remove(Path.GetFullPath(Path.Combine(projectDirectory, pattern)));
            return;
        }

        foreach (string file in EnumeratePattern(projectDirectory, pattern))
        {
            files.Remove(file);
        }
    }

    private static IEnumerable<string> EnumeratePattern(string projectDirectory, string pattern)
    {
        string normalized = pattern.Replace('/', Path.DirectorySeparatorChar);
        int wildcardIndex = normalized.IndexOfAny(['*', '?']);
        int separatorIndex = normalized.LastIndexOf(Path.DirectorySeparatorChar, wildcardIndex);
        string relativeRoot = separatorIndex < 0 ? string.Empty : normalized[..separatorIndex];
        string searchPattern = separatorIndex < 0 ? normalized : normalized[(separatorIndex + 1)..];
        string root = Path.GetFullPath(Path.Combine(projectDirectory, relativeRoot));
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .Select(Path.GetFullPath);
    }

    private string ResolvePath(string path) => Path.GetFullPath(Path.IsPathRooted(path)
        ? path
        : Path.Combine(_workspacePath, path));

    private string ToRelativePath(string path) => Path.GetRelativePath(_workspacePath, path)
        .Replace(Path.DirectorySeparatorChar, '/');

    private static bool ContainsWildcard(string path) => path.IndexOfAny(['*', '?']) >= 0;

    private static bool IsSupportedProject(string path) =>
        Path.GetExtension(path) is ".csproj" or ".vbproj";

    private static bool IsIgnoredPath(string path)
    {
        string[] segments = Path.GetFullPath(path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => segment is "bin" or "obj" or ".git" or ".vs");
    }
}
