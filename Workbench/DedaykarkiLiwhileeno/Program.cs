// See https://aka.ms/new-console-template for more information

using Windows.Win32;
using Windows.Win32.Storage.FileSystem;

if(args.Length == 0)
{
    Console.WriteLine($"请输入要删除的文件的路径");
}

var file = args[0];
file = Path.GetFullPath(file);

if (!File.Exists(file))
{
    Console.WriteLine($"找不到 '{file}' 文件");
}

PInvoke.MoveFileEx(file, null, MOVE_FILE_FLAGS.MOVEFILE_DELAY_UNTIL_REBOOT);

Console.WriteLine($"重启后删除 '{file}' 文件");
