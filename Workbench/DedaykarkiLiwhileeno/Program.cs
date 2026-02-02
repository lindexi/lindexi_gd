// See https://aka.ms/new-console-template for more information

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

Console.WriteLine($"重启后删除 '{file}' 文件");
