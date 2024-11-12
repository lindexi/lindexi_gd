// See https://aka.ms/new-console-template for more information

using Lindexi.Src.GitCommand;

var git = new Git(new DirectoryInfo("."));

var logCommit = git.GetLogCommit("39fa705ead59b2e23220c810c138ee41f6553f87", "aac7465d7c166f77958f6d0d04635f4b8e9eb27b");
Console.WriteLine($"LogCommit={logCommit.Length}");

var currentCommit = git.GetCurrentCommit();
Console.WriteLine($"CurrentCommit={currentCommit}");

Console.WriteLine("Hello, World!");
