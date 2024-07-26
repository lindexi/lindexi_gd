// See https://aka.ms/new-console-template for more information

using System.Threading.Channels;

var source = @"E:\程序\";
var target = @"D:\程序\";

if (args.Length == 2)
{
    source = args[0];
    target = args[1];
}

var copyFinishHashSet = new HashSet<string>();
var logFile = "Log.txt";
var logErrorFile = "LogError.txt";
var channel = Channel.CreateUnbounded<string>();
_ = Task.Run(async () =>
{
    while (true)
    {
        var result = await channel.Reader.WaitToReadAsync();
        if (!result)
        {
            return;
        }

        if (channel.Reader.TryRead(out var filePath))
        {
            Console.WriteLine($"Copy {filePath}");
            File.AppendAllLines(logFile, [filePath]);
        }
    }
});

CopyFolder(source, target);


void CopyFolder(string sourceFolder, string targetFolder)
{
    if (!Directory.Exists(targetFolder))
    {
        Directory.CreateDirectory(targetFolder);
    }

    foreach (var file in Directory.GetFiles(sourceFolder))
    {
        if (!copyFinishHashSet.Add(file))
        {
            continue;
        }

        var targetFile = Path.Join(targetFolder, Path.GetFileName(file));
        //if (File.Exists(targetFile))
        //{
        //    var sourceFileHash = GetFileHash(file);
        //    var targetFileHash = GetFileHash(targetFile);
        //    if (sourceFileHash == targetFileHash)
        //    {
        //        continue;
        //    }
        //}

        try
        {
            File.Copy(file, targetFile, true);
            channel.Writer.TryWrite(file);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            File.AppendAllLines(logErrorFile, [$"Copy '{file}' Fail {e}"]);
        }
    }

    foreach (var folder in Directory.GetDirectories(sourceFolder))
    {
        try
        {
            var targetFolderName = Path.Join(targetFolder, Path.GetFileName(folder));
            CopyFolder(folder, targetFolderName);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            File.AppendAllLines(logErrorFile, [$"Copy '{folder}' Fail {e}"]);
        }
    }
}