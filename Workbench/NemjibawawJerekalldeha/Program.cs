// See https://aka.ms/new-console-template for more information
var folder = @"F:\vs2022离线下载2024年7月15日\";

var fileList = new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories).ToList();
var file = fileList.MaxBy(t => t.Length);

Console.WriteLine("Hello, World!");
