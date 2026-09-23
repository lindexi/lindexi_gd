#:property TargetFramework=net10.0

using System.Collections.ObjectModel;

return ServicesFileOrganizer.Run(args);

internal static class ServicesFileOrganizer
{
    private const string ApplyOption = "--apply";
    private const string SourceOption = "--source";

    private static readonly ReadOnlyDictionary<string, string> s_destinations = new
    (
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CodingAgentChatRunner.cs"] = "Chat",
            ["CodingChatRunOptions.cs"] = "Chat",
            ["ICodingChatRunner.cs"] = "Chat",
            ["LoopIterationChatReducer.cs"] = "Chat",
            ["CodingChatHistoryLoader.cs"] = "Sessions",
            ["ICodingChatSessionStore.cs"] = "Sessions",
            ["CodingChatSettingsService.cs"] = "Settings",
            ["CodingChatShellSettings.cs"] = "Settings",
            ["CodingChatApplication.cs"] = "WorkTasks",
            ["CodingChatRuntime.cs"] = "WorkTasks",
            ["CodingChatStartup.cs"] = "WorkTasks",
            ["WorkTaskStore.cs"] = "WorkTasks",
            ["CodingWorkspaceController.cs"] = "Workspace",
            ["OpenAIModelCatalogClient.cs"] = Path.Combine("Integrations", "OpenAI"),
            ["WindowsSandboxConnectionTester.cs"] = Path.Combine("Integrations", "WindowsSandbox"),
            ["TemporaryImageViewer.cs"] = "Shell",
        }
    );

    public static int Run(string[] args)
    {
        try
        {
            Options options = ParseOptions(args);
            if (options.ShowHelp)
            {
                return 0;
            }

            string sourceDirectory = Path.GetFullPath(options.SourceDirectory);
            if (!Directory.Exists(sourceDirectory))
            {
                Console.Error.WriteLine($"源目录不存在：{sourceDirectory}");
                return 2;
            }

            IReadOnlyList<MoveOperation> operations = CreatePlan(sourceDirectory);
            PrintPlan(sourceDirectory, operations, options.ApplyChanges);
            if (!options.ApplyChanges)
            {
                Console.WriteLine();
                Console.WriteLine($"当前为预览模式。确认无误后添加 {ApplyOption} 执行移动。");
                return 0;
            }

            ValidatePlan(operations);
            ApplyPlan(operations);
            Console.WriteLine();
            Console.WriteLine($"已移动 {operations.Count(operation => operation.Status == MoveStatus.Pending)} 个文件。");
            return 0;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            PrintUsage();
            return 2;
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"文件整理失败：{exception.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.Error.WriteLine($"没有足够权限完成文件整理：{exception.Message}");
            return 1;
        }
    }

    private static Options ParseOptions(string[] args)
    {
        bool applyChanges = false;
        string? sourceDirectory = null;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            if (string.Equals(argument, ApplyOption, StringComparison.OrdinalIgnoreCase))
            {
                applyChanges = true;
                continue;
            }

            if (string.Equals(argument, SourceOption, StringComparison.OrdinalIgnoreCase))
            {
                if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
                {
                    throw new ArgumentException($"{SourceOption} 后必须提供目录路径。");
                }

                sourceDirectory = args[index];
                continue;
            }

            if (argument is "--help" or "-h" or "/?")
            {
                PrintUsage();
                return new Options(ResolveDefaultSourceDirectory(), false, ShowHelp: true);
            }

            throw new ArgumentException($"无法识别参数：{argument}");
        }

        return new Options(sourceDirectory ?? ResolveDefaultSourceDirectory(), applyChanges, ShowHelp: false);
    }

    private static string ResolveDefaultSourceDirectory()
    {
        string currentDirectory = Environment.CurrentDirectory;
        string directCandidate = Path.Combine(currentDirectory, "CodingChatRoom.AvaloniaShell", "Services");
        if (Directory.Exists(directCandidate))
        {
            return directCandidate;
        }

        string parentCandidate = Path.GetFullPath
        (
            Path.Combine(currentDirectory, "..", "CodingChatRoom.AvaloniaShell", "Services")
        );
        return parentCandidate;
    }

    private static IReadOnlyList<MoveOperation> CreatePlan(string sourceDirectory)
    {
        var operations = new List<MoveOperation>(s_destinations.Count);
        foreach ((string fileName, string relativeDestinationDirectory) in s_destinations)
        {
            string sourcePath = Path.Combine(sourceDirectory, fileName);
            string destinationPath = Path.Combine(sourceDirectory, relativeDestinationDirectory, fileName);
            MoveStatus status = File.Exists(sourcePath)
                ? MoveStatus.Pending
                : File.Exists(destinationPath)
                    ? MoveStatus.AlreadyOrganized
                    : MoveStatus.Missing;
            operations.Add(new MoveOperation(sourcePath, destinationPath, status));
        }

        return operations;
    }

    private static void PrintPlan
    (
        string sourceDirectory,
        IReadOnlyList<MoveOperation> operations,
        bool applyChanges
    )
    {
        Console.WriteLine($"源目录：{sourceDirectory}");
        Console.WriteLine($"模式：{(applyChanges ? "执行" : "预览")}");
        Console.WriteLine();

        foreach (MoveOperation operation in operations)
        {
            string source = Path.GetRelativePath(sourceDirectory, operation.SourcePath);
            string destination = Path.GetRelativePath(sourceDirectory, operation.DestinationPath);
            string status = operation.Status switch
            {
                MoveStatus.Pending => "移动",
                MoveStatus.AlreadyOrganized => "已整理",
                MoveStatus.Missing => "未找到",
                _ => throw new InvalidOperationException("未知的移动状态。"),
            };
            Console.WriteLine($"[{status}] {source} -> {destination}");
        }
    }

    private static void ValidatePlan(IReadOnlyList<MoveOperation> operations)
    {
        foreach (MoveOperation operation in operations.Where(operation => operation.Status == MoveStatus.Pending))
        {
            if (File.Exists(operation.DestinationPath))
            {
                throw new IOException($"目标文件已存在，不会覆盖：{operation.DestinationPath}");
            }
        }
    }

    private static void ApplyPlan(IReadOnlyList<MoveOperation> operations)
    {
        foreach (MoveOperation operation in operations.Where(operation => operation.Status == MoveStatus.Pending))
        {
            string destinationDirectory = Path.GetDirectoryName(operation.DestinationPath)
                ?? throw new InvalidOperationException($"无法确定目标目录：{operation.DestinationPath}");
            Directory.CreateDirectory(destinationDirectory);
            File.Move(operation.SourcePath, operation.DestinationPath);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("用法：");
        Console.WriteLine("  dotnet run ServicesFileOrganizer.cs");
        Console.WriteLine("  dotnet run ServicesFileOrganizer.cs -- --apply");
        Console.WriteLine("  dotnet run ServicesFileOrganizer.cs -- --source <Services目录> [--apply]");
    }

    private sealed record Options(string SourceDirectory, bool ApplyChanges, bool ShowHelp);

    private sealed record MoveOperation(string SourcePath, string DestinationPath, MoveStatus Status);

    private enum MoveStatus
    {
        Pending,
        AlreadyOrganized,
        Missing,
    }
}
