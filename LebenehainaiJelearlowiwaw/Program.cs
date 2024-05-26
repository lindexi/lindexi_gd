﻿// See https://aka.ms/new-console-template for more information
using System.IO.Compression;
using System.Text;

// 测试两个部分
// 加入某个文件夹
// 加入某个文件夹但是不要某个文件

var zipFile = "1.zip";
using (var fileStream = new FileStream(zipFile, FileMode.Create, FileAccess.Write))
{
    using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: true/*自己释放 FileStream 对象*/, Encoding.UTF8);
    Foo.AppendDirectoryToZipArchive(zipArchive, @"C:\lindexi\Library\", "Lib");
    Foo.AppendDirectoryToZipArchive(zipArchive, @"C:\lindexi\CA\", "Pem", fileCanAddedPredicate: filePath =>
    {
        var fileName = Path.GetFileName(filePath);
        return fileName != "foo.ignore.file";
    });
}

Console.WriteLine("Hello, World!");

class Foo
{
    /// <summary>
    /// 追加文件夹到压缩文件里面
    /// </summary>
    /// <param name="archive"></param>
    /// <param name="sourceDirectoryName"></param>
    /// <param name="zipRelativePath">在压缩包里面的相对路径</param>
    /// <param name="compressionLevel"></param>
    /// <param name="fileCanAddedPredicate"></param>
    public static void AppendDirectoryToZipArchive(ZipArchive archive, string sourceDirectoryName, string zipRelativePath, CompressionLevel compressionLevel = CompressionLevel.Fastest, Predicate<string>? fileCanAddedPredicate = null)
    {
        var folders = new Stack<string>();

        folders.Push(sourceDirectoryName);

        while (folders.Count > 0)
        {
            var currentFolder = folders.Pop();

            foreach (var item in Directory.EnumerateFiles(currentFolder))
            {
                if (fileCanAddedPredicate != null && !fileCanAddedPredicate(item))
                {
                    continue;
                }

                archive.CreateEntryFromFile(item, Path.Join(zipRelativePath, Path.GetRelativePath(sourceDirectoryName, item)), compressionLevel);
            }

            foreach (var item in Directory.EnumerateDirectories(currentFolder))
            {
                folders.Push(item);
            }
        }
    }
}