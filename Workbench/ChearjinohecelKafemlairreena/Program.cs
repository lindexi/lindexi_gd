// See https://aka.ms/new-console-template for more information

using LibGit2Sharp;

var folder = @"C:\lindexi\Code\lindexi\.git\";

var repository = new Repository(folder, new RepositoryOptions()
{
    
});

bool isPathIgnored = repository.Ignore.IsPathIgnored("bin/obj/Foo.exe");

var queryableCommitLog = repository.Commits;
Commit commit = queryableCommitLog.First();

ObjectId commitId = commit.Id;
GitObject gitObject = repository.Lookup(commitId);

TreeChanges treeChanges = repository.Diff.Compare<TreeChanges>(commit.Parents.First().Tree,commit.Tree);
foreach (TreeEntryChanges treeEntryChanges in treeChanges)
{
    var path = treeEntryChanges.Path;
    var changeKind = treeEntryChanges.Status;
}

HistoryDivergence historyDivergence = repository.ObjectDatabase.CalculateHistoryDivergence(queryableCommitLog.Skip(100).First(), commit);
var historyDivergenceCommonAncestor = historyDivergence.CommonAncestor;

GC.KeepAlive(repository);
Console.WriteLine("Hello, World!");
