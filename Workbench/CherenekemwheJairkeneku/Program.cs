using System;
using System.IO;

const string taskRootFolder = @"C:\lindexi\Work\BatchTask";
const string slideComparisonFileName = "SlideComparison.pdf";

if (!Directory.Exists(taskRootFolder))
{
    Console.WriteLine($"目录不存在: {taskRootFolder}");
    return;
}

var copiedFileCount = 0;

foreach (var taskFolder in Directory.GetDirectories(taskRootFolder))
{
    var taskFolderName = Path.GetFileName(taskFolder);
    var sourceFilePath = Path.Combine(taskFolder, slideComparisonFileName);

    if (!File.Exists(sourceFilePath))
    {
        Console.WriteLine($"跳过，未找到文件: {sourceFilePath}");
        continue;
    }

    var targetFileName = $"SlideComparison_{taskFolderName}.pdf";
    var targetFilePath = Path.Combine(taskRootFolder, targetFileName);

    File.Copy(sourceFilePath, targetFilePath, overwrite: true);
    copiedFileCount++;

    Console.WriteLine($"已拷贝: {sourceFilePath} -> {targetFilePath}");
}

Console.WriteLine($"完成，共拷贝 {copiedFileCount} 个文件。");
