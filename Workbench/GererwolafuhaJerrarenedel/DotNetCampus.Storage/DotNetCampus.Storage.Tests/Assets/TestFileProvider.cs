using System;
using System.IO;

namespace DotNetCampus.Storage.Tests.Assets;

internal static class TestFileProvider
{
    public static string GetTestFilePath(string fileName)
    {
        if (File.Exists(fileName))
        {
            return Path.GetFullPath(fileName);
        }

        return System.IO.Path.Join(AppContext.BaseDirectory, "Assets", "TestFiles", fileName);
    }

    public static FileInfo GetTestFile(string fileName)
    {
        return new FileInfo(GetTestFilePath(fileName));
    }
}
