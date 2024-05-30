// See https://aka.ms/new-console-template for more information

using Lindexi.Src.GitCommand;

var git = new Git(new DirectoryInfo("."));
Console.WriteLine(git.GetCurrentCommit());

Console.WriteLine("Hello, World!");
