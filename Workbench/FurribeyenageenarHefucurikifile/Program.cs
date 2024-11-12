// See https://aka.ms/new-console-template for more information

using Lindexi.Src.GitCommand;

var git = new Git(new DirectoryInfo("."));

var logCommit = git.GetLogCommit();
Console.WriteLine($"LogCommit={logCommit.Length}");

var currentCommit = git.GetCurrentCommit();
Console.WriteLine($"CurrentCommit={currentCommit}");

Console.WriteLine("Hello, World!");
