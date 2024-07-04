// See https://aka.ms/new-console-template for more information

using System.Threading.Channels;

var source = @"E:\程序";
var target = @"D:\lindexi\Code\";

var channel = Channel.CreateUnbounded<string>();

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
    WriteLog($"{folderPath} ->  {targetPath}");

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
        if (File.Exists(targetFile))
        {
            WriteLog($"{sourceFile} ->\r\n  {targetFile} [FileExists]");
        }
        else
        {
            File.Copy(sourceFile, targetFile);
            WriteLog($"{sourceFile} ->\r\n  {targetFile} [Success]");
        }
    }
    catch (Exception e)
    {
        WriteLog($"{sourceFile} ->\r\n  {targetFile} [Exception] {e}");
    }
}

void WriteLog(string message)
{
    channel.Writer.TryWrite(message);
}