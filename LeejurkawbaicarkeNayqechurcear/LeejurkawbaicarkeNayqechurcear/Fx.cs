using System.IO;

namespace LeejurkawbaicarkeNayqechurcear;

static class PackageDirectory
{
    internal static void Copy(string runtimeSourceFolder, string runtimeTargetFolder)
    {
        throw new System.NotImplementedException();
    }
}

static class BuildConfiguration
{
    public static string OutputDirectory { get; internal set; }
    public static string BuildConfigurationDirectory { get; internal set; }
}

class Fx
{
    private void F1()
    {
        CopyDotNetRuntimeFolder();
    }

    /// <summary>
    /// 使用自己分发的运行时，需要从 Build\dotnet runtime\runtime 拷贝
    /// </summary>
    private void CopyDotNetRuntimeFolder()
    {
        var runtimeTargetFolder = Path.Combine(BuildConfiguration.OutputDirectory, "runtime");
        var runtimeSourceFolder =
            Path.Combine(BuildConfiguration.BuildConfigurationDirectory, @"dotnet runtime\runtime");
        PackageDirectory.Copy(runtimeSourceFolder, runtimeTargetFolder);
    }

}
