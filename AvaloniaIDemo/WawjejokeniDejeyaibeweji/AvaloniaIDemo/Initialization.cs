using LibDemo;

namespace AvaloniaIDemo;

public static class Initializer
{
    public static void InitAssembly()
    {
        Runner.SetAppRunner(new AppRunner());
    }
}

file class AppRunner : IAppRunner
{
    public void Run()
    {
        Program.Main([]);
    }
}