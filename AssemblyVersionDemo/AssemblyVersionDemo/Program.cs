// See https://aka.ms/new-console-template for more information

using System.Reflection;

Assembly? assembly = null;

assembly ??= Assembly.GetEntryAssembly();
assembly ??= Assembly.GetCallingAssembly();

var version = assembly.GetName().Version;
var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

var informationalVersion =
    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

Console.WriteLine("Hello, World!");
