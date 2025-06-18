using System.Diagnostics;
using Microsoft.DotNet.Archive;

namespace DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

public static class DirectoryArchive
{
    public static void Compress(DirectoryInfo inputDirectoryInfo, FileInfo outputFileInfo)
    {
        using var outputFileStream = new FileStream(outputFileInfo.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        FileInfo[] fileArray = inputDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);

        var headStream = new MemoryStream();
        long totalFileLength = 0;

        for (int i = 0; i < sizeof(long); i++)
        {
            // 预留的内容，用来后续填充长度信息
            headStream.WriteByte(0xFF);
        }

        var streamWriter = new StreamWriter(headStream);
        foreach (var fileInfo in fileArray)
        {
            totalFileLength += fileInfo.Length;
            var relativePath = Path.GetRelativePath(inputDirectoryInfo.FullName, fileInfo.FullName);
            streamWriter.WriteLine($"{relativePath}");
            streamWriter.WriteLine($"Length={fileInfo.Length}");
            streamWriter.WriteLine();
        }
        streamWriter.Flush();
        var headLength = headStream.Position;
        Debug.Assert(headStream.Position == headStream.Length);
        totalFileLength += headLength;

        headStream.Position = 0;
        for (int i = 0; i < sizeof(long); i++)
        {
            headStream.WriteByte((byte) (headLength >> (8 * i)));
        }
        headStream.Position = 0;

        var currentIndex = 0;
        FileStream? currentFileStream = null;

        var directoryArchiveProxyInputStream = new DirectoryArchiveProxyInputStream(headStream, totalFileLength);
        directoryArchiveProxyInputStream.ReadNext += (_, args) =>
        {
            // ReSharper disable AccessToDisposedClosure
            if (currentFileStream is not null && !ReferenceEquals(currentFileStream, args.CurrentInputStream))
            {
                throw new Exception();
            }

            while (currentIndex < fileArray.Length)
            {
                FileInfo fileInfo = fileArray[currentIndex];
                if (fileInfo.Length == 0)
                {
                    // 跳过空文件
                    currentIndex++;
                    continue;
                }
                else
                {
                    break;
                }
            }

            if (currentIndex < fileArray.Length)
            {
                args.CurrentInputStream.Dispose();

                FileInfo fileInfo = fileArray[currentIndex];
                var fileStream = fileInfo.OpenRead();
                currentFileStream = fileStream;
                args.UpdateInputStream(fileStream);

                Console.WriteLine($"读取文件中 {currentIndex}/{fileArray.Length} 文件：{fileInfo}");

                currentIndex++;
            }
        };

        var stopwatch = Stopwatch.StartNew();

        CompressionUtility.Compress(directoryArchiveProxyInputStream, outputFileStream, new ConsoleProgressReport());

        stopwatch.Stop();
        Console.WriteLine($"TotalLength={totalFileLength};Elapsed={stopwatch.Elapsed.Minutes}m,{stopwatch.Elapsed.Seconds}s,{stopwatch.Elapsed.Milliseconds}ms");

        if (currentFileStream is not null)
        {
            currentFileStream.Dispose();
        }
    }

    public static void Decompress(FileInfo archiveFileInfo, DirectoryInfo outputFolder)
    {
        using var archiveFileStream = archiveFileInfo.OpenRead();
        using var directoryArchiveProxyOutputStream = new DirectoryArchiveProxyOutputStream(outputFolder);

        // 解压缩 130MB 只需 5 秒
        var stopwatch = Stopwatch.StartNew();
        CompressionUtility.Decompress(archiveFileStream, directoryArchiveProxyOutputStream, new Progress<ProgressReport>());
        Console.WriteLine($"Elapsed={stopwatch.Elapsed.Minutes}m,{stopwatch.Elapsed.Seconds}s,{stopwatch.Elapsed.Milliseconds}ms");
    }
}