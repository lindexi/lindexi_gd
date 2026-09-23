#:property TargetFramework=net10.0

return FileOrganizer.Run(args);

internal static class FileOrganizer
{
    private const string ApplyOption = "--apply";
    private const string SourceOption = "--source";

    private static readonly IReadOnlyList<FileMove> s_moves =
    [
        new("CodingChatRoom.AvaloniaShell/Services/CodingAgentChatRunner.cs", "CodingChatRoom.AvaloniaShell/Services/Chat/CodingAgentChatRunner.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingChatRunOptions.cs", "CodingChatRoom.AvaloniaShell/Services/Chat/CodingChatRunOptions.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/ICodingChatRunner.cs", "CodingChatRoom.AvaloniaShell/Services/Chat/ICodingChatRunner.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/LoopIterationChatReducer.cs", "CodingChatRoom.AvaloniaShell/Services/Chat/LoopIterationChatReducer.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingChatHistoryLoader.cs", "CodingChatRoom.AvaloniaShell/Services/Sessions/CodingChatHistoryLoader.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/ICodingChatSessionStore.cs", "CodingChatRoom.AvaloniaShell/Services/Sessions/ICodingChatSessionStore.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingChatSettingsService.cs", "CodingChatRoom.AvaloniaShell/Services/Settings/CodingChatSettingsService.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingChatShellSettings.cs", "CodingChatRoom.AvaloniaShell/Services/Settings/CodingChatShellSettings.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/WorkTaskStore.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/WorkTaskStore.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingWorkspaceController.cs", "CodingChatRoom.AvaloniaShell/Services/Workspace/CodingWorkspaceController.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/OpenAIModelCatalogClient.cs", "CodingChatRoom.AvaloniaShell/Services/Integrations/OpenAI/OpenAIModelCatalogClient.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/WindowsSandboxConnectionTester.cs", "CodingChatRoom.AvaloniaShell/Services/Integrations/WindowsSandbox/WindowsSandboxConnectionTester.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/TemporaryImageViewer.cs", "CodingChatRoom.AvaloniaShell/Services/Shell/TemporaryImageViewer.cs"),

        new("CodingChatRoom.AvaloniaShell/Services/CodingChatApplication.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingWorkTaskController.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingChatApplication.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingWorkTaskController.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingChatRuntime.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntime.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingChatRuntime.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntime.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/CodingChatStartup.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntimeFactory.cs"),
        new("CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingChatStartup.cs", "CodingChatRoom.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntimeFactory.cs"),

        new("CodingChatRoom.AvaloniaShell.Tests/CodingChatApplicationTests.cs", "CodingChatRoom.AvaloniaShell.Tests/CodingWorkTaskControllerTests.cs"),
        new("CodingChatRoom.AvaloniaShell.Tests/CodingChatApplicationTestFactory.cs", "CodingChatRoom.AvaloniaShell.Tests/CodingWorkTaskControllerTestFactory.cs"),
        new("CodingChatRoom.AvaloniaShell.Tests/CodingChatStartupTests.cs", "CodingChatRoom.AvaloniaShell.Tests/CodingWorkTaskRuntimeFactoryTests.cs"),
    ];

    public static int Run(string[] args)
    {
        try
        {
            Options options = ParseOptions(args);
            if (options.ShowHelp)
            {
                return 0;
            }

            string codeDirectory = Path.GetFullPath(options.SourceDirectory);
            if (!Directory.Exists(codeDirectory))
            {
                Console.Error.WriteLine($"Code 目录不存在：{codeDirectory}");
                return 2;
            }

            IReadOnlyList<MoveOperation> operations = CreatePlan(codeDirectory);
            PrintPlan(codeDirectory, operations, options.ApplyChanges);
            if (!options.ApplyChanges)
            {
                Console.WriteLine();
                Console.WriteLine($"当前为预览模式。确认无误后添加 {ApplyOption} 执行移动和改名。");
                return 0;
            }

            ValidatePlan(operations);
            ApplyPlan(operations);
            Console.WriteLine();
            Console.WriteLine($"已移动或改名 {operations.Count(operation => operation.Status == MoveStatus.Pending)} 个文件。");
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
                    throw new ArgumentException($"{SourceOption} 后必须提供 Code 目录路径。");
                }

                sourceDirectory = args[index];
                continue;
            }

            if (argument is "--help" or "-h" or "/?")
            {
                PrintUsage();
                return new Options(ResolveDefaultCodeDirectory(), false, true);
            }

            throw new ArgumentException($"无法识别参数：{argument}");
        }

        return new Options(sourceDirectory ?? ResolveDefaultCodeDirectory(), applyChanges, false);
    }

    private static string ResolveDefaultCodeDirectory()
    {
        string currentDirectory = Environment.CurrentDirectory;
        if (Directory.Exists(Path.Combine(currentDirectory, "CodingChatRoom.AvaloniaShell")))
        {
            return currentDirectory;
        }

        string parentDirectory = Path.GetFullPath(Path.Combine(currentDirectory, ".."));
        return Directory.Exists(Path.Combine(parentDirectory, "CodingChatRoom.AvaloniaShell"))
            ? parentDirectory
            : currentDirectory;
    }

    private static IReadOnlyList<MoveOperation> CreatePlan(string codeDirectory)
    {
        var operations = new List<MoveOperation>(s_moves.Count);
        var claimedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (FileMove move in s_moves)
        {
            string sourcePath = Path.GetFullPath(Path.Combine(codeDirectory, move.SourceRelativePath));
            string destinationPath = Path.GetFullPath(Path.Combine(codeDirectory, move.DestinationRelativePath));
            MoveStatus status;
            if (File.Exists(sourcePath) && claimedDestinations.Add(destinationPath))
            {
                status = MoveStatus.Pending;
            }
            else if (File.Exists(destinationPath))
            {
                status = MoveStatus.AlreadyOrganized;
            }
            else
            {
                status = MoveStatus.Missing;
            }

            operations.Add(new MoveOperation(sourcePath, destinationPath, status));
        }

        return operations;
    }

    private static void PrintPlan(string codeDirectory, IReadOnlyList<MoveOperation> operations, bool applyChanges)
    {
        Console.WriteLine($"Code 目录：{codeDirectory}");
        Console.WriteLine($"模式：{(applyChanges ? "执行" : "预览")}");
        Console.WriteLine();

        foreach (MoveOperation operation in operations)
        {
            string status = operation.Status switch
            {
                MoveStatus.Pending => "移动/改名",
                MoveStatus.AlreadyOrganized => "已完成",
                MoveStatus.Missing => "未找到",
                _ => throw new InvalidOperationException("未知文件状态。"),
            };
            Console.WriteLine
            (
                $"[{status}] {Path.GetRelativePath(codeDirectory, operation.SourcePath)} -> " +
                Path.GetRelativePath(codeDirectory, operation.DestinationPath)
            );
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
        Console.WriteLine("  dotnet run ServicesFileOrganizer.cs -- --source <Code目录> [--apply]");
    }

    private sealed record Options(string SourceDirectory, bool ApplyChanges, bool ShowHelp);
    private sealed record FileMove(string SourceRelativePath, string DestinationRelativePath);
    private sealed record MoveOperation(string SourcePath, string DestinationPath, MoveStatus Status);

    private enum MoveStatus
    {
        Pending,
        AlreadyOrganized,
        Missing,
    }
}
