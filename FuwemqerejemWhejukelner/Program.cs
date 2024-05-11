// See https://aka.ms/new-console-template for more information

using LibGit2Sharp;

var path = Path.GetFullPath(@"..\..\..\..\");
var repository = new Repository(path);
var commit = repository.Commits.First();
Console.WriteLine(commit.Sha); // 第一个就是最后一个

Console.WriteLine(repository.Head.FriendlyName);
var commitCount = repository.Commits.Count();



Console.WriteLine("Hello, World!");
