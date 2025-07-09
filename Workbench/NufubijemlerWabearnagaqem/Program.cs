// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

Console.WriteLine($"RuntimeFeature.IsDynamicCodeSupported={RuntimeFeature.IsDynamicCodeSupported}");

Console.WriteLine($"Assembly.GetEntryAssembly() is not null={Assembly.GetEntryAssembly() is not null}");
try
{
    Console.WriteLine($"Assembly.GetCallingAssembly() is not null={Assembly.GetCallingAssembly() is not null}");
}
catch (Exception e)
{
    Console.WriteLine($"Assembly.GetCallingAssembly() Exception. {e.GetType().FullName}: {e.Message}");
}

Console.WriteLine($"Assembly.GetExecutingAssembly() is not null={Assembly.GetExecutingAssembly() is not null}");

Console.WriteLine($"Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>() is not null={Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>() is not null}");

Console.WriteLine("Hello, World!");

// AOT 输出：
/*
RuntimeFeature.IsDynamicCodeSupported=False
Assembly.GetEntryAssembly() is not null=True
Assembly.GetCallingAssembly() Exception. System.PlatformNotSupportedException: Operation is not supported on this platform.
Assembly.GetExecutingAssembly() is not null=True
Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>() is not null=False
 */
