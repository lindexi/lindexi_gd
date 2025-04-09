using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Path = System.IO.Path;

namespace CardawnarheaCahichemga;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var zipCompareOptions = new ZipCompareOptions(ReturnFast: true, IgnoreExtra: false);

            var result = await ZipComparer.Compare(new FileInfo(ZipFilePathTextBox.Text), new DirectoryInfo(UnzipFolderPathTextBox.Text), zipCompareOptions);

            if (result.IsSuccess)
            {
                MessageBox.Show("文件一致");
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("文件不一致");
                foreach (var item in result.DifferenceList)
                {
                    stringBuilder.AppendLine(item.RelativeFilePath);
                    stringBuilder.AppendLine(item.DifferenceType.ToString());
                }
                MessageBox.Show(stringBuilder.ToString());
            }
        }
        catch
        {
            // 忽略
        }
    }
}

public static class ZipComparer
{
    public static async Task<ZipCompareResult> Compare(FileInfo zipFile, DirectoryInfo unzipFolder, ZipCompareOptions options)
    {
        HashSet<string/*RelativePath*/>? visitedFileSet = null;
        if (!options.IgnoreExtra)
        {
            visitedFileSet = [];
        }

        List<ZipCompareDifferenceFileInfo>? differenceList = null;

        await using var fileStream = zipFile.OpenRead();
        using var zipArchive = new System.IO.Compression.ZipArchive(fileStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
        foreach (var zipArchiveEntry in zipArchive.Entries)
        {
            var name = zipArchiveEntry.FullName;
            visitedFileSet?.Add(name);
            var filePath = Path.Join(unzipFolder.FullName, name);

            if (!File.Exists(filePath))
            {
                // 文件不存在
                AddDifference(new ZipCompareDifferenceFileInfo(name, ZipCompareDifferenceType.Miss));

                if (options.ReturnFast)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length != zipArchiveEntry.Length)
            {
                // 文件大小不同
                AddDifference(new ZipCompareDifferenceFileInfo(name, ZipCompareDifferenceType.ContentLengthDifference));
                if (options.ReturnFast)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }

            // 先不引入 Crc32 对比逻辑。要真正想用，需要引入 System.IO.Hashing 包
            //var crc32 = zipArchiveEntry.Crc32;
            //if (crc32 != 0 && Crc32.IsSupported)
            //{
            //    var fileCrc32 = await GetFileZipCrcAsync(fileInfo);
            //    if (crc32 == fileCrc32)
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        // CRC32 不同
            //        AddDifference(new ZipCompareDifferenceFileInfo(name, ZipCompareDifferenceType.ContentDifference));
            //        if (options.ReturnFast)
            //        {
            //            break;
            //        }
            //        else
            //        {
            //            continue;
            //        }
            //    }
            //}

            // 开始对比文件内容
            await using var zipStream = zipArchiveEntry.Open();
            await using var currentFileStream = fileInfo.OpenRead();
            var success = await CompareStream(zipStream, currentFileStream);
            if (!success)
            {
                // 文件内容不同
                AddDifference(new ZipCompareDifferenceFileInfo(name, ZipCompareDifferenceType.ContentDifference));
                if (options.ReturnFast)
                {
                    break;
                }
            }
        }

        var isDifferenceFastReturn = options.ReturnFast && differenceList is { Count: > 0 };

        if (!options.IgnoreExtra
            // 如果是快速返回，则不需要检查额外的文件，此时已经存在不相同的文件
            && !isDifferenceFastReturn)
        {
            // 如果不能忽略额外的文件，则需要检查解压缩文件夹中是否有额外的文件
            Debug.Assert(visitedFileSet != null);

            foreach (var file in unzipFolder.EnumerateFiles("*", new EnumerationOptions()
            {
                RecurseSubdirectories = true
            }))
            {
                var relativePath = Path.GetRelativePath(unzipFolder.FullName, file.FullName);
                if (!visitedFileSet.Contains(relativePath))
                {
                    // 额外的文件
                    AddDifference(new ZipCompareDifferenceFileInfo(relativePath, ZipCompareDifferenceType.Extra));
                    if (options.ReturnFast)
                    {
                        break;
                    }
                }
            }
        }

        return new ZipCompareResult
        {
            DifferenceList = differenceList,
        };

        void AddDifference(ZipCompareDifferenceFileInfo info)
        {
            differenceList ??= new List<ZipCompareDifferenceFileInfo>();
            differenceList.Add(info);
        }
    }

    //private static async ValueTask<uint> GetFileZipCrcAsync(FileInfo fileInfo)
    //{
    //    const int bufferLength = 4 * 1024;
    //    var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
    //    uint crc = 0;
    //    try
    //    {
    //        await using var fileStream = fileInfo.OpenRead();
    //        var memory = buffer.AsMemory(0, bufferLength);

    //        while (true)
    //        {
    //            var readLength = await fileStream.ReadAsync(memory);

    //            if (readLength == 0)
    //            {
    //                return crc;
    //            }

    //            for (int i = 0; i < readLength; i++)
    //            {
    //                crc = Crc32.ComputeCrc32(crc, memory.Span[i]);
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        ArrayPool<byte>.Shared.Return(buffer);
    //    }
    //}

    private static async ValueTask<bool> CompareStream(Stream a, Stream b)
    {
        const int bufferLength = 4 * 1024;
        var bufferA = ArrayPool<byte>.Shared.Rent(bufferLength);
        var bufferB = ArrayPool<byte>.Shared.Rent(bufferLength);

        try
        {
            while (true)
            {
                var memoryA = bufferA.AsMemory(0, bufferLength);

                var readLength = await a.ReadAsync(memoryA);
                if (readLength == 0)
                {
                    // 读取完毕
                    return true;
                }

                var memoryB = bufferB.AsMemory(0, readLength);
                await b.ReadExactlyAsync(memoryB);

                if (!memoryA.Span.Slice(0, readLength).SequenceEqual(memoryB.Span))
                {
                    return false;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bufferA);
            ArrayPool<byte>.Shared.Return(bufferB);
        }
    }
}

/// <summary>
/// 比较的选项
/// </summary>
/// <param name="ReturnFast">找到第一个差异就立刻结束</param>
/// <param name="IgnoreExtra">忽略额外的文件。忽略解压缩文件夹存在，但压缩包不存在的文件</param>
public readonly record struct ZipCompareOptions(bool ReturnFast, bool IgnoreExtra);

public readonly record struct ZipCompareResult
{
    [MemberNotNullWhen(false, nameof(DifferenceList))]
    public bool IsSuccess => DifferenceList is null || DifferenceList.Count == 0;
    public IReadOnlyList<ZipCompareDifferenceFileInfo>? DifferenceList { get; internal init; }
}

public readonly record struct ZipCompareDifferenceFileInfo(string RelativeFilePath, ZipCompareDifferenceType DifferenceType);

public enum ZipCompareDifferenceType
{
    /// <summary>
    /// 文件不存在
    /// </summary>
    Miss,

    /// <summary>
    /// 文件大小不同
    /// </summary>
    ContentLengthDifference,

    /// <summary>
    /// 文件内容不同
    /// </summary>
    ContentDifference,

    /// <summary>
    /// 额外的文件，即解压缩文件夹存在，但压缩包不存在的文件
    /// </summary>
    Extra,
}