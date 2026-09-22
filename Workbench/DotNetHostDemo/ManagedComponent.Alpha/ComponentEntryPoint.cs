namespace ManagedComponent.Alpha;

public delegate int ComponentEntryPoint(string[] args);

public static class Component
{
    public static int Run(string[] args)
    {
        Console.WriteLine($"Alpha component is running on .NET {Environment.Version}.");
        Console.WriteLine($"Arguments: {string.Join(", ", args)}");
        return 101;
    }
}
