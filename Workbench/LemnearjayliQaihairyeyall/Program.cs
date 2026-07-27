using System.Diagnostics;
using GitignoreParserNet;

string experimentRoot = Path.Join(Path.GetTempPath(), "GitignoreParserNetExploration");

if (Directory.Exists(experimentRoot))
{
    Directory.Delete(experimentRoot, recursive: true);
}

Directory.CreateDirectory(experimentRoot);

Scenario scenario = Scenario.Create(experimentRoot);
scenario.Materialize();

Console.WriteLine($"Git 仓库: {scenario.RepositoryRoot}");
Console.WriteLine($"扫描起点（自身无 .gitignore）: {scenario.ScanRoot}");
Console.WriteLine("图例: OK=与 Git 一致, DIFF=与 Git 不一致, ignored=true 表示被排除");
Console.WriteLine();

var matcher = new LayeredGitignoreMatcher(scenario.ScanRoot);
int differenceCount = 0;

foreach (Candidate candidate in scenario.Candidates)
{
    MatchResult parserResult = matcher.Match(candidate.FullPath, candidate.IsDirectory);
    GitMatchResult gitResult = GitCheckIgnore.Match(scenario.RepositoryRoot, candidate.FullPath);
    bool isSame = parserResult.IsIgnored == gitResult.IsIgnored;

    if (!isSame)
    {
        differenceCount++;
    }

    Console.WriteLine($"[{(isSame ? "OK" : "DIFF")}] {candidate.RelativePath}{(candidate.IsDirectory ? "/" : string.Empty)}");
    Console.WriteLine($"  Parser: ignored={parserResult.IsIgnored,-5} {parserResult.RuleDescription}");
    Console.WriteLine($"  Git:    ignored={gitResult.IsIgnored,-5} {gitResult.RuleDescription}");
}

Console.WriteLine();
Console.WriteLine($"完成: {scenario.Candidates.Count} 个候选项，{differenceCount} 个差异。");
Console.WriteLine("可修改 Scenario.Create 中的规则和候选项后反复执行 dotnet run。");

internal sealed class LayeredGitignoreMatcher
{
    private readonly string _rootDirectory;
    private readonly IReadOnlyList<IgnoreRule> _rules;

    public LayeredGitignoreMatcher(string currentDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);

        _rootDirectory = FindRepositoryRoot(Path.GetFullPath(currentDirectory));
        _rules = LoadRules(_rootDirectory);
    }

    public MatchResult Match(string path, bool isDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        EnsurePathIsUnderRoot(fullPath);

        DirectoryInfo? ancestor = Directory.GetParent(fullPath);
        while (ancestor is not null && IsUnderOrEqual(ancestor.FullName, _rootDirectory))
        {
            MatchResult ancestorResult = MatchDirect(ancestor.FullName, isDirectory: true);
            if (ancestorResult.IsIgnored)
            {
                return new MatchResult(true, $"父目录已排除: {Path.GetRelativePath(_rootDirectory, ancestor.FullName)}");
            }

            if (PathsEqual(ancestor.FullName, _rootDirectory))
            {
                break;
            }

            ancestor = ancestor.Parent;
        }

        return MatchDirect(fullPath, isDirectory);
    }

    private MatchResult MatchDirect(string fullPath, bool isDirectory)
    {

        bool isIgnored = false;
        IgnoreRule? decidingRule = null;

        foreach (IgnoreRule rule in _rules)
        {
            if (!IsUnderOrEqual(fullPath, rule.BaseDirectory))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(rule.BaseDirectory, fullPath).Replace('\\', '/');
            if (isDirectory)
            {
                relativePath += '/';
            }

            if (!rule.Parser.Inspects(relativePath))
            {
                continue;
            }

            isIgnored = rule.Parser.Denies(relativePath);
            decidingRule = rule;
        }

        string description = decidingRule is null
            ? "未被任何规则匹配"
            : $"最后规则={Path.GetRelativePath(_rootDirectory, decidingRule.GitignorePath)}:{decidingRule.LineNumber}:{decidingRule.Pattern}";

        return new MatchResult(isIgnored, description);
    }

    private static IReadOnlyList<IgnoreRule> LoadRules(string rootDirectory)
    {
        string[] gitignoreFiles = Directory.GetFiles(rootDirectory, ".gitignore", SearchOption.AllDirectories);
        Array.Sort(gitignoreFiles, static (left, right) =>
        {
            int depthComparison = GetDepth(left).CompareTo(GetDepth(right));
            return depthComparison != 0
                ? depthComparison
                : StringComparer.OrdinalIgnoreCase.Compare(left, right);
        });

        var rules = new List<IgnoreRule>();
        foreach (string gitignorePath in gitignoreFiles)
        {
            string baseDirectory = Path.GetDirectoryName(gitignorePath)!;
            string[] lines = File.ReadAllLines(gitignorePath);

            for (int index = 0; index < lines.Length; index++)
            {
                string pattern = lines[index];
                if (string.IsNullOrWhiteSpace(pattern) || pattern.TrimStart().StartsWith('#'))
                {
                    continue;
                }

                rules.Add(new IgnoreRule(
                    baseDirectory,
                    gitignorePath,
                    index + 1,
                    pattern,
                    new GitignoreParser(pattern)));
            }
        }

        return rules;
    }

    private static string FindRepositoryRoot(string currentDirectory)
    {
        DirectoryInfo? directory = new(currentDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Join(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"未能从当前目录向上找到 Git 仓库: {currentDirectory}");
    }

    private static int GetDepth(string path)
    {
        int depth = 0;
        foreach (char character in path)
        {
            if (character is '\\' or '/')
            {
                depth++;
            }
        }

        return depth;
    }

    private void EnsurePathIsUnderRoot(string path)
    {
        if (!IsUnderOrEqual(path, _rootDirectory))
        {
            throw new ArgumentException($"路径必须位于匹配根目录之下: {path}", nameof(path));
        }
    }

    private static bool IsUnderOrEqual(string path, string directory)
    {
        string relativePath = Path.GetRelativePath(directory, path);
        return relativePath != ".."
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(left),
            Path.TrimEndingDirectorySeparator(right),
            StringComparison.OrdinalIgnoreCase);
}

internal static class GitCheckIgnore
{
    public static GitMatchResult Match(string repositoryRoot, string path)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("check-ignore");
        startInfo.ArgumentList.Add("--verbose");
        startInfo.ArgumentList.Add("--no-index");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(Path.GetRelativePath(repositoryRoot, path));

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动 git check-ignore。");

        string output = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        return process.ExitCode switch
        {
            0 => new GitMatchResult(!IsNegatedMatch(output), output),
            1 => new GitMatchResult(false, "未被任何规则匹配"),
            _ => throw new InvalidOperationException($"git check-ignore 失败，退出码 {process.ExitCode}: {error}"),
        };
    }

    private static bool IsNegatedMatch(string output)
    {
        int firstColon = output.IndexOf(':');
        int secondColon = firstColon < 0 ? -1 : output.IndexOf(':', firstColon + 1);
        int tab = secondColon < 0 ? -1 : output.IndexOf('\t', secondColon + 1);

        return secondColon >= 0
            && tab > secondColon
            && output.AsSpan(secondColon + 1, tab - secondColon - 1).StartsWith("!", StringComparison.Ordinal);
    }
}

internal sealed record Scenario(string RepositoryRoot, string ScanRoot, IReadOnlyList<FileEntry> Files, IReadOnlyList<Candidate> Candidates)
{
    public static Scenario Create(string experimentRoot)
    {
        string repositoryRoot = Path.Join(experimentRoot, "repo");
        string workRoot = Path.Join(repositoryRoot, "work");
        string scanRoot = Path.Join(workRoot, "scan");

        FileEntry[] files =
        [
            new(".gitignore", "*.root.tmp\n/work/scan/root-only/\nshared/\n!shared/keep.txt\n**/temp?.dat\nrange[0-2].bin\n"),
            new("work/.gitignore", "*.log\nscan/build/\n!important.log\nanchored.txt\nfolder-only/\n"),
            new("work/scan/readme.txt", "readme"),
            new("work/scan/inherited.root.tmp", "root temp"),
            new("work/scan/root-only/file.txt", "root only"),
            new("work/scan/shared/drop.txt", "drop"),
            new("work/scan/shared/keep.txt", "keep attempt"),
            new("work/scan/debug.log", "log"),
            new("work/scan/important.log", "important"),
            new("work/scan/anchored.txt", "anchored"),
            new("work/scan/nested/anchored.txt", "nested anchored"),
            new("work/scan/build/output.dll", "build output"),
            new("work/scan/temp1.dat", "question wildcard"),
            new("work/scan/a/b/temp9.dat", "double star"),
            new("work/scan/a/b/temp10.dat", "not a single character"),
            new("work/scan/range0.bin", "range"),
            new("work/scan/range3.bin", "outside range"),
            new("work/scan/folder-only/data.txt", "directory only"),
            new("work/scan/src/.gitignore", "!important.log\n*.generated.cs\n/local-only/\norder.txt\n!order.txt\norder.txt\n"),
            new("work/scan/src/important.log", "nested important"),
            new("work/scan/src/code.generated.cs", "generated"),
            new("work/scan/src/code.cs", "source"),
            new("work/scan/src/order.txt", "last rule wins"),
            new("work/scan/src/local-only/data.txt", "local"),
            new("work/scan/src/deep/code.generated.cs", "deep generated"),
            new("work/scan/src/deep/.gitignore", "!code.generated.cs\n*.cache\n"),
            new("work/scan/src/deep/code.cache", "cache"),
            new("work/scan/src/deep/visible.txt", "visible"),
        ];

        Candidate[] candidates =
        [
            Candidate.File(repositoryRoot, "work/scan/readme.txt"),
            Candidate.File(repositoryRoot, "work/scan/inherited.root.tmp"),
            Candidate.Directory(repositoryRoot, "work/scan/root-only"),
            Candidate.File(repositoryRoot, "work/scan/root-only/file.txt"),
            Candidate.Directory(repositoryRoot, "work/scan/shared"),
            Candidate.File(repositoryRoot, "work/scan/shared/drop.txt"),
            Candidate.File(repositoryRoot, "work/scan/shared/keep.txt"),
            Candidate.File(repositoryRoot, "work/scan/debug.log"),
            Candidate.File(repositoryRoot, "work/scan/important.log"),
            Candidate.File(repositoryRoot, "work/scan/anchored.txt"),
            Candidate.File(repositoryRoot, "work/scan/nested/anchored.txt"),
            Candidate.Directory(repositoryRoot, "work/scan/build"),
            Candidate.File(repositoryRoot, "work/scan/build/output.dll"),
            Candidate.File(repositoryRoot, "work/scan/temp1.dat"),
            Candidate.File(repositoryRoot, "work/scan/a/b/temp9.dat"),
            Candidate.File(repositoryRoot, "work/scan/a/b/temp10.dat"),
            Candidate.File(repositoryRoot, "work/scan/range0.bin"),
            Candidate.File(repositoryRoot, "work/scan/range3.bin"),
            Candidate.Directory(repositoryRoot, "work/scan/folder-only"),
            Candidate.File(repositoryRoot, "work/scan/src/important.log"),
            Candidate.File(repositoryRoot, "work/scan/src/code.generated.cs"),
            Candidate.File(repositoryRoot, "work/scan/src/code.cs"),
            Candidate.File(repositoryRoot, "work/scan/src/order.txt"),
            Candidate.Directory(repositoryRoot, "work/scan/src/local-only"),
            Candidate.File(repositoryRoot, "work/scan/src/local-only/data.txt"),
            Candidate.File(repositoryRoot, "work/scan/src/deep/code.generated.cs"),
            Candidate.File(repositoryRoot, "work/scan/src/deep/code.cache"),
            Candidate.File(repositoryRoot, "work/scan/src/deep/visible.txt"),
        ];

        return new Scenario(repositoryRoot, scanRoot, files, candidates);
    }

    public void Materialize()
    {
        Directory.CreateDirectory(RepositoryRoot);
        InitializeGitRepository();

        foreach (FileEntry entry in Files)
        {
            string fullPath = Path.Join(RepositoryRoot, entry.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, entry.Content);
        }
    }

    private void InitializeGitRepository()
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("init");
        startInfo.ArgumentList.Add("--quiet");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动 git init。");

        string error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git init 失败，退出码 {process.ExitCode}: {error}");
        }
    }
}

internal sealed record FileEntry(string RelativePath, string Content);

internal sealed record Candidate(string FullPath, string RelativePath, bool IsDirectory)
{
    public static Candidate File(string root, string relativePath) =>
        new(Path.Join(root, relativePath), relativePath.Replace('\\', '/'), false);

    public static Candidate Directory(string root, string relativePath) =>
        new(Path.Join(root, relativePath), relativePath.Replace('\\', '/'), true);
}

internal sealed record IgnoreRule(
    string BaseDirectory,
    string GitignorePath,
    int LineNumber,
    string Pattern,
    GitignoreParser Parser);

internal readonly record struct MatchResult(bool IsIgnored, string RuleDescription);

internal readonly record struct GitMatchResult(bool IsIgnored, string RuleDescription);