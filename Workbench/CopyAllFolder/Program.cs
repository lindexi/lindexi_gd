// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Threading.Channels;

var source = @"E:\程序";
var target = @"D:\lindexi\Code\";

var logFile = "LogFile.txt";
var stopwatch = Stopwatch.StartNew();

var finishFileHashSet = new HashSet<string>();
if (File.Exists(logFile))
{
    foreach (var line in File.ReadLines(logFile))
    {
        finishFileHashSet.Add(line);
    }
}

var channel = Channel.CreateUnbounded<SyncInfo>();
int fileCount = 0;

Task.Run(async () =>
{
    while (true)
    {
        var result = await channel.Reader.WaitToReadAsync();
        if (!result)
        {
            break;
        }

        var message = await channel.Reader.ReadAsync();
        Console.WriteLine(message);

        try
        {
            File.AppendAllText(logFile, $"{message.Source}\n");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
});

CopyFolder(source);

Console.WriteLine("Hello, World!");

void CopyFolder(string folderPath)
{
    var relativePath = Path.GetRelativePath(source, folderPath);
    var targetPath = Path.Join(target, relativePath);
    targetPath = Path.GetFullPath(targetPath);
    Directory.CreateDirectory(targetPath);
    //WriteLog($"{folderPath} ->  {targetPath}");

    foreach (var file in Directory.EnumerateFiles(folderPath))
    {
        var fileRelativePath = Path.GetRelativePath(source, file);
        var targetFilePath = Path.Join(target, fileRelativePath);

        CopyFile(file, targetFilePath);
    }

    foreach (var folder in Directory.EnumerateDirectories(folderPath))
    {
        CopyFolder(folder);
    }
}

void CopyFile(string sourceFile, string targetFile)
{
    try
    {
        if (finishFileHashSet.Contains(sourceFile) || File.Exists(targetFile)
            //&& GetFileLength(sourceFile) == GetFileLength(targetFile)
            )
        {
            WriteLog(new SyncInfo(sourceFile, targetFile, SyncEnum.Skip, null));
            return;
        }

        File.Copy(sourceFile, targetFile);
        WriteLog(new SyncInfo(sourceFile, targetFile, SyncEnum.Success, null));

        fileCount++;
        if (fileCount > 10000 || stopwatch.Elapsed > TimeSpan.FromSeconds(10))
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            fileCount = 0;
            stopwatch.Restart();
        }
    }
    catch (Exception e)
    {
        WriteLog(new SyncInfo(sourceFile, targetFile, SyncEnum.Fail, e));
    }

    long GetFileLength(string file)
    {
        return new FileInfo(file).Length;
    }
}

void WriteLog(SyncInfo info)
{
    channel.Writer.TryWrite(info);
}

record SyncInfo(string Source, string Target, SyncEnum SyncEnum, Exception? Exception)
{
    public override string ToString()
    {
        return $"{Source} ->\r\n  {Target} [{SyncEnum}] {Exception}";
    }
}

enum SyncEnum
{
    Success,
    Skip,
    Fail,
}