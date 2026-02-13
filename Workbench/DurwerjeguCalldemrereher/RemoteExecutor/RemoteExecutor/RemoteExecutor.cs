using System.Diagnostics;
using System.Reflection;
using DotNetCampus.Cli;

namespace RemoteExecutors;

public static class RemoteExecutor
{
    public static void Invoke(Action action)
    {
        var method = action.Method;

        Type? type = method.DeclaringType;

        if (type is null)
        {
            throw new ArgumentException();
        }

        TypeInfo typeInfo = IntrospectionExtensions.GetTypeInfo(type);

        var methodName = method.Name;
        var className = typeInfo.FullName!;
        var assemblyFullName = typeInfo.Assembly.FullName!;

        string[] commandLineArgs = 
            [
                RemoteExecutorOption.CommandName,
                "--AssemblyName", assemblyFullName,
                "--ClassName", className,
                "--MethodName", methodName,
                "--Pid", Environment.ProcessId.ToString(),
            ];

        //TryHandle(commandLineArgs);
        var processPath = Environment.ProcessPath;
        if (processPath is null)
        {
            throw new InvalidOperationException();
        }

        var process = Process.Start(processPath,commandLineArgs);
        process.WaitForExit();
    }

    public static bool TryHandle(string[] commandLineArgs)
    {
        var index = commandLineArgs.IndexOf(RemoteExecutorOption.CommandName);
        if (index == -1)
        {
            return false;
        }

        // 解析命令行参数
        // 只取后面的一部分参数
        var optionCommandLineArgs = commandLineArgs.Skip(index+1).ToList();
        var result = CommandLine.Parse(optionCommandLineArgs)
            .AddHandler<RemoteExecutorOption>(option =>
            {
                var assemblyName = option.AssemblyName;
                var className = option.ClassName;
                var methodName = option.MethodName;

                var assembly = Assembly.Load(assemblyName);
                var classType = assembly.GetType(className)!;

                var methodInfo = classType.GetTypeInfo().GetDeclaredMethod(methodName)!;
                object? instance = null;
                if (!methodInfo.IsStatic)
                {
                    instance = Activator.CreateInstance(classType);
                }
                object? result = methodInfo.Invoke(instance, null);
                _ = result;
            })
            .Run();

        _ = result;

        return true;
    }
}