// See https://aka.ms/new-console-template for more information

using Lindexi.Src.GitCommand;

var git = new Git(new DirectoryInfo("."));

var currentCommit = git.GetCurrentCommit();
Console.WriteLine($"CurrentCommit={currentCommit}");

Console.WriteLine("Hello, World!");
