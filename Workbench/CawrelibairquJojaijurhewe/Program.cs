// See https://aka.ms/new-console-template for more information


var path = @"H:\lindexi\test.txt";
var isUDiskPath = IsUDiskPath(path);
Console.WriteLine($"Path={path} 是 U 盘={isUDiskPath}");

foreach (var driveInfo in DriveInfo.GetDrives())
{
    if (driveInfo.DriveType == DriveType.Removable)
    {
        Console.WriteLine($"{driveInfo.RootDirectory} 是 U 盘");
    }
}
Console.WriteLine("Hello, World!");

static bool IsUDiskPath(string path)
{
    if (!Path.IsPathFullyQualified(path))
    {
        throw new ArgumentException($"路径必须是绝对路径。 Path={path}", nameof(path));
    }

    var pathRoot = Path.GetPathRoot(path);
    if (pathRoot is null)
    {
        return false;
    }

    var driveInfo = new DriveInfo(pathRoot);
    // 必须先判断 IsReady 属性，详细请看 [C# 获取磁盘或硬盘信息的坑，存在未就绪（IsReady = false）导致异常的问题 - wuty007 - 博客园](https://www.cnblogs.com/wuty/p/18413323 )
    return driveInfo.IsReady && driveInfo.DriveType == DriveType.Removable;
}