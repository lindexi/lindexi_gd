// See https://aka.ms/new-console-template for more information

var folder = args[0];

foreach (var file in Directory.GetFiles(folder))
{
    File.SetLastWriteTime(file, DateTime.Now);
}

Console.WriteLine("Hello, World!");
