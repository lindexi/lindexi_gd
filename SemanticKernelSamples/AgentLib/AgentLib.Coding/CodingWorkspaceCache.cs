using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CodingWorkspaceCache : IAsyncDisposable
{
    private static readonly string[] DefaultExcludedDirectoryNames =
    [
        ".git",
        ".vs",
        "artifacts",
        "bin",
        "obj",
        "TestResults",
    ];

    private IAsyncDisposable? _ownedResource;

    internal CodingWorkspaceCache
    (
        string workspacePath,
        IReadOnlyList<ToolRegistration> toolRegistrations,
        IAsyncDisposable? ownedResource = null
    )
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("工作区路径不能为空。", nameof(workspacePath));
        }

        ArgumentNullException.ThrowIfNull(toolRegistrations);
        WorkspacePath = Path.GetFullPath(workspacePath);
        ToolRegistrations = Array.AsReadOnly(toolRegistrations.ToArray());
        Tools = Array.AsReadOnly(ToolRegistrations.Select(static registration => registration.Tool).ToArray());
        ToolRegistrationRegistry = new ToolRegistrationRegistry(ToolRegistrations);
        _ownedResource = ownedResource;
    }

    public string WorkspacePath { get; }

    public WorkspaceToolProvider? WorkspaceTools { get; private init; }

    public DotNetCliTools? DotNetCliTools { get; private init; }

    public DotNetApiTools? DotNetApiTools { get; private init; }

    public CodingWorkspaceContentTools? ContentTools { get; private init; }

    public RoslynAgentTools? RoslynTools { get; private init; }

    public IReadOnlyList<ToolRegistration> ToolRegistrations { get; }

    public IReadOnlyList<AITool> Tools { get; }

    public ToolRegistrationRegistry ToolRegistrationRegistry { get; }

    public CodingRunWorkspaceContext CreateRunContext(
        IReadOnlyList<ToolRegistration> additionalToolRegistrations,
        bool enableDotNetRun = false)
    {
        ArgumentNullException.ThrowIfNull(additionalToolRegistrations);
        ToolRegistration[] optionalRegistrations = enableDotNetRun
            ? [DotNetCliTools!.CreateRunToolRegistration()]
            : [];
        if (additionalToolRegistrations.Count == 0 && optionalRegistrations.Length == 0)
        {
            return new CodingRunWorkspaceContext(WorkspacePath, Tools, ToolRegistrationRegistry);
        }

        ToolRegistration[] registrations =
            [.. ToolRegistrations, .. optionalRegistrations, .. additionalToolRegistrations];
        AITool[] tools = [.. registrations.Select(static registration => registration.Tool)];
        return new CodingRunWorkspaceContext(
            WorkspacePath,
            Array.AsReadOnly(tools),
            new ToolRegistrationRegistry(registrations));
    }

    public Task<bool> StopLanguageServerAsync() =>
        RoslynTools?.StopLanguageServerAsync() ?? Task.FromResult(false);

    public static CodingWorkspaceCache Create(
        string workspacePath,
        string languageServerCommand)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            throw new ArgumentException("工作区路径不能为空。", nameof(workspacePath));
        }

        if (string.IsNullOrWhiteSpace(languageServerCommand))
        {
            throw new ArgumentException("Roslyn Language Server 命令不能为空。", nameof(languageServerCommand));
        }

        string fullWorkspacePath = Path.GetFullPath(workspacePath);
        if (!Directory.Exists(fullWorkspacePath))
        {
            throw new DirectoryNotFoundException($"指定的代码工作区不存在: {fullWorkspacePath}");
        }

        var workspaceTools = new WorkspaceToolProvider
        {
            AllowReadingOutsideWorkspace = true,
            WorkspacePath = fullWorkspacePath,
        };
        foreach (string directoryName in DefaultExcludedDirectoryNames)
        {
            workspaceTools.ExcludedDirectoryNames.Add(directoryName);
        }

        var dotNetCliTools = new DotNetCliTools(fullWorkspacePath);
        var dotNetApiTools = new DotNetApiTools(fullWorkspacePath);
        var contentTools = new CodingWorkspaceContentTools(fullWorkspacePath);
        var roslynTools = new RoslynAgentTools(fullWorkspacePath, languageServerCommand);
        IReadOnlyList<ToolRegistration> registrations =
        [
            .. roslynTools.AsToolRegistrations(),
            .. workspaceTools.CreateDefaultToolRegistrations(),
            .. dotNetCliTools.AsToolRegistrations(),
            .. dotNetApiTools.AsToolRegistrations(),
            .. contentTools.AsToolRegistrations(),
        ];
        return new CodingWorkspaceCache(fullWorkspacePath, registrations, roslynTools)
        {
            WorkspaceTools = workspaceTools,
            DotNetCliTools = dotNetCliTools,
            DotNetApiTools = dotNetApiTools,
            ContentTools = contentTools,
            RoslynTools = roslynTools,
        };
    }

    public ValueTask DisposeAsync()
    {
        IAsyncDisposable? ownedResource = Interlocked.Exchange(ref _ownedResource, null);
        return ownedResource?.DisposeAsync() ?? default;
    }
}

internal sealed record CodingRunWorkspaceContext(
    string? WorkspacePath,
    IReadOnlyList<AITool> Tools,
    ToolRegistrationRegistry ToolRegistrationRegistry)
{
    public static CodingRunWorkspaceContext Empty { get; } =
        new(null, [], ToolRegistrationRegistry.Empty);
}