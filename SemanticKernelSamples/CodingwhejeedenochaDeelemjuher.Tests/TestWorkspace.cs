namespace CodingwhejeedenochaDeelemjuher.Tests;

internal sealed class TestWorkspace : IDisposable
{
    public TestWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), $"RoslynAgentToolsTests-{Guid.NewGuid():N}");
        ProjectDirectory = Path.Combine(RootPath, "SampleApp");
        Directory.CreateDirectory(ProjectDirectory);

        SolutionPath = Path.Combine(RootPath, "Sample.slnx");
        ProjectPath = Path.Combine(ProjectDirectory, "SampleApp.csproj");
        SourcePath = Path.Combine(ProjectDirectory, "Calculator.cs");
        UsagePath = Path.Combine(ProjectDirectory, "Program.cs");

        File.WriteAllText(SolutionPath, """
            <Solution>
              <Project Path="SampleApp/SampleApp.csproj" />
            </Solution>
            """);
        File.WriteAllText(ProjectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(SourcePath, """
            namespace SampleApp;

            public interface ICalculator
            {
                int Add(int left, int right);
            }

            public sealed class Calculator : ICalculator
            {
                public int Add(int left, int right) => left + right;
            }
            """);
        File.WriteAllText(UsagePath, """
            using SampleApp;

            ICalculator calculator = new Calculator();
            Console.WriteLine(calculator.Add(1, 2));
            """);
    }

    public string RootPath { get; }

    public string SolutionPath { get; }

    public string ProjectDirectory { get; }

    public string ProjectPath { get; }

    public string SourcePath { get; }

    public string UsagePath { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(RootPath, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
