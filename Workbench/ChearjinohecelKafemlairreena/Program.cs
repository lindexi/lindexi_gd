// See https://aka.ms/new-console-template for more information

using LibGit2Sharp;

var folder = @"C:\lindexi\Code\lindexi\.git\";

var repository = new Repository(folder, new RepositoryOptions()
{
    
});

bool isPathIgnored = repository.Ignore.IsPathIgnored("bin/obj/Foo.exe");

var queryableCommitLog = repository.Commits;
Commit commit = queryableCommitLog.First();

var remoteCollection = repository.Network.Remotes;

HistoryDivergence historyDivergence = repository.ObjectDatabase.CalculateHistoryDivergence(queryableCommitLog.Skip(100).First(), commit);
var historyDivergenceCommonAncestor = historyDivergence.CommonAncestor;


GC.KeepAlive(repository);
Console.WriteLine("Hello, World!");
