// See https://aka.ms/new-console-template for more information

using DotNetCampus.Cli;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using InstallerSevenZipTool;

if (args.Length == 0)
{
    Console.WriteLine("""
                      安装包资产制作工具
                      
                      InstallerSevenZipTool -f <输入文件夹> -o <输出文件>
                      """);
    return;
}

var options = CommandLine.Parse(args).As<Options>();
var inputDirectory = Path.GetFullPath(options.InputDirectory);

var outputFile = Path.GetFullPath(options.OutputFile);

Console.WriteLine($"开始制作安装包资产文件。输入文件夹： '{inputDirectory}' 输出文件： '{outputFile}'");

DirectoryArchive.Compress(new DirectoryInfo(inputDirectory), new FileInfo(outputFile));

if (options.IgnoreChecksum is not true)
{
    Console.WriteLine($"完成压缩，开始测试压缩包是否正确");
    // 测试的方法就是解压缩到临时目录，然后检查文件是否完整
    var workingFolder = Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}");

    DirectoryArchive.Decompress(new FileInfo(outputFile), Directory.CreateDirectory(workingFolder));

    // 解压缩完成之后，执行文件对比
    foreach (var originFile in Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(inputDirectory, originFile);
        var unzipFile = Path.Join(workingFolder, relativePath);

        if (!File.Exists(unzipFile))
        {
            Console.WriteLine($"压缩过程出错，找不到 {relativePath} 文件");
            return;
        }

        var buffer1 = new byte[4096];
        var buffer2 = new byte[4096];

        CompareFile();

        void CompareFile()
        {
            using var file1Stream = File.OpenRead(originFile);
            using var file2Stream = File.OpenRead(unzipFile);

            if (file1Stream.Length != file2Stream.Length)
            {
                Console.WriteLine($"文件 {relativePath} 内容不一致，压缩过程出错");
                return;
            }

            while (true)
            {
                var readCount1 = file1Stream.Read(buffer1, 0, buffer1.Length);
                var readCount2 = file2Stream.Read(buffer2, 0, buffer2.Length);
                if (readCount1 != readCount2 || !buffer1.AsSpan(0, readCount1).SequenceEqual(buffer2.AsSpan(0, readCount2)))
                {
                    Console.WriteLine($"文件 {relativePath} 内容不一致，压缩过程出错");
                    return;
                }
                if (readCount1 == 0)
                {
                    break; // 文件读取完毕
                }
            }
        }
    }

    // 删除临时目录
    Directory.Delete(workingFolder, true);
}

Console.WriteLine($"安装包资产文件创建成功： {outputFile}");
