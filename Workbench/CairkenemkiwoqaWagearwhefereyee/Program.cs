// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using LibGit2Sharp;

AppendLog($"进程启动 {Environment.ProcessId} 工作路径 {Environment.CurrentDirectory}");

var codeFolder = new DirectoryInfo(@"C:\lindexi\Code");

var folder = @$"{codeFolder.EnumerateDirectories().First(t => t.Name.Contains("lindexi")).FullName}\lindexi\.git\";

AppendLog($"仓库地址 {folder}");

var repository = new Repository(folder);

foreach (var worktree in repository.Worktrees)
{
    if (worktree.Name.Contains("Text"))
    {
        var last = worktree.WorktreeRepository.Commits.First();
        var message = last.Message;

        var lastCommitFile = Path.Join(AppContext.BaseDirectory, "LastCommit.txt");
        var lastCommit = string.Empty;
        if (File.Exists(lastCommitFile))
        {
            lastCommit = File.ReadAllText(lastCommitFile);
        }

        if (lastCommit != last.Sha)
        {
            File.WriteAllText(lastCommitFile, last.Sha);
            Update(message);
        }
    }
}

static void Update(string message)
{
    var gitFolder = @"C:\lindexi\Work\Code\.git\";
    var sourceFolder = @"C:\lindexi\Work\Source\";

    var repository = new Repository(gitFolder);
    foreach (Worktree? worktree in repository.Worktrees)
    {
        if (worktree is null)
        {
            continue;
        }

        if (worktree.Name.Contains("Text"))
        {
            Process.Start(new ProcessStartInfo(@"C:\lindexi\Application\同步文档代码.lnk")
            {
                UseShellExecute = true
            })!.WaitForExit();

            var worktreeRepository = worktree.WorktreeRepository;
            
            //worktreeRepository.Index.Add(".");

            Process.Start(new ProcessStartInfo("git")
            {
                ArgumentList =
                {
                    "add",
                    "."
                },
                WorkingDirectory = sourceFolder
            })!.WaitForExit();

            var signature = new Signature("lindexi", "lindexi_gd@163.com", DateTimeOffset.Now.AddHours(2));
            worktreeRepository.Commit(message, signature, signature);

            return;
        }
    }
}

Console.WriteLine("Hello, World!");

static void AppendLog(string message)
{
    var logFile = Path.Join(AppContext.BaseDirectory, "Log.txt");
    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] {message}\r\n");
}
