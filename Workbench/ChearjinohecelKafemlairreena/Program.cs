// See https://aka.ms/new-console-template for more information

using LibGit2Sharp;

var folder = @"C:\lindexi\Code\lindexi\.git\";

var repository = new Repository(folder, new RepositoryOptions()
{
    
});

bool isPathIgnored = repository.Ignore.IsPathIgnored("bin/obj/Foo.exe");

var queryableCommitLog = repository.Commits;
Commit commit = queryableCommitLog.First();

var logEntries = queryableCommitLog.QueryBy("Workbench/ChearjinohecelKafemlairreena/Program.cs");
foreach (LogEntry logEntry in logEntries)
{
}

ObjectId commitId = commit.Id;
GitObject gitObject = repository.Lookup(commitId);
var patch = repository.Diff.Compare<Patch>(commit.Parents.First().Tree, commit.Tree);

foreach (PatchEntryChanges patchEntryChanges in patch)
{
    var path = patchEntryChanges.Path;
    foreach (var addedLine in patchEntryChanges.AddedLines)
    {
    }

    string patchText = patchEntryChanges.Patch;
    /*
    diff --git a/Workbench/ChearjinohecelKafemlairreena/Program.cs b/Workbench/ChearjinohecelKafemlairreena/Program.cs
    index 63d0e46..6a80004 100644
    --- a/Workbench/ChearjinohecelKafemlairreena/Program.cs
    +++ b/Workbench/ChearjinohecelKafemlairreena/Program.cs
    @@ -14,11 +14,18 @@ bool isPathIgnored = repository.Ignore.IsPathIgnored("bin/obj/Foo.exe");
     var queryableCommitLog = repository.Commits;
     Commit commit = queryableCommitLog.First();

    -var remoteCollection = repository.Network.Remotes;
    +ObjectId commitId = commit.Id;
    +GitObject gitObject = repository.Lookup(commitId);
    +
    +TreeChanges treeChanges = repository.Diff.Compare<TreeChanges>(commit.Parents.First().Tree,commit.Tree);
    +foreach (TreeEntryChanges treeEntryChanges in treeChanges)
    +{
    +    var path = treeEntryChanges.Path;
    +    var changeKind = treeEntryChanges.Status;
    +}

     HistoryDivergence historyDivergence = repository.ObjectDatabase.CalculateHistoryDivergence(queryableCommitLog.Skip(100).First(), commit);
     var historyDivergenceCommonAncestor = historyDivergence.CommonAncestor;

    -
     GC.KeepAlive(repository);
     Console.WriteLine("Hello, World!");

     */
}

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
