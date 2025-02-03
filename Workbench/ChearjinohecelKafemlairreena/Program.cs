// See https://aka.ms/new-console-template for more information

using LibGit2Sharp;

var folder = @"C:\lindexi\Code\lindexi\.git\";

var repository = new Repository(folder, new RepositoryOptions()
{
    
});

var queryableCommitLog = repository.Commits;
Commit commit = queryableCommitLog.First();

var remoteCollection = repository.Network.Remotes;

GC.KeepAlive(repository);
Console.WriteLine("Hello, World!");
