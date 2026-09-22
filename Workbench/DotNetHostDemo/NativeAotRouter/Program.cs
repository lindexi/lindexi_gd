if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: NativeAotRouter <alpha|beta> [component arguments]");
    return 64;
}

ComponentDescriptor? component = args[0].ToLowerInvariant() switch
{
    "alpha" => new(
        "ManagedComponent.Alpha",
        "ManagedComponent.Alpha.Component",
        "ManagedComponent.Alpha.ComponentEntryPoint"),
    "beta" => new(
        "ManagedComponent.Beta",
        "ManagedComponent.Beta.Component",
        "ManagedComponent.Beta.ComponentEntryPoint"),
    _ => null
};

if (component is null)
{
    Console.Error.WriteLine($"Unknown component '{args[0]}'. Expected alpha or beta.");
    return 64;
}

string baseDirectory = AppContext.BaseDirectory;
string[] componentArguments = args.Skip(1).ToArray();
return HostFxrRunner.Run(
    Path.Combine(baseDirectory, $"{component.AssemblyName}.runtimeconfig.json"),
    Path.Combine(baseDirectory, $"{component.AssemblyName}.dll"),
    $"{component.TypeName}, {component.AssemblyName}",
    "Run",
    $"{component.DelegateTypeName}, {component.AssemblyName}",
    componentArguments);

internal sealed record ComponentDescriptor(string AssemblyName, string TypeName, string DelegateTypeName);
