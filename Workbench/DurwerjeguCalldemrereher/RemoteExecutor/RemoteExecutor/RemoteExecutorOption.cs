using DotNetCampus.Cli.Compiler;

namespace RemoteExecutors;

internal class RemoteExecutorOption
{
    public const string CommandName = "RemoteExecutor_F6679170-3719-49AB-9936-7CAB5AB6294D";
    //public const string AssemblyNameOption = "--AssemblyName";
    //public const string ClassNameOption = "--ClassName";
    //public const string MethodNameOption = "--MethodName";

    [Option]
    public required string AssemblyName { get; init; }

    [Option]
    public required string ClassName { get; init; }

    [Option]
    public required string MethodName { get; init; }

    [Option]
    public required int Pid { get; init; }
}