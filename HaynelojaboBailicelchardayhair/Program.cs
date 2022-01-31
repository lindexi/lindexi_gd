using System;
using System.IO;
using dotnetCampus.Cli;
using dotnetCampus.DotNETBuild.Utils;
using dotnetCampus.GitCommand;
using GitLabApiClient;
using GitLabApiClient.Models.MergeRequests.Requests;

namespace HaynelojaboBailicelchardayhair
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = dotnetCampus.Cli.CommandLine.Parse(args).As<Options>();

            var directoryInfo = new DirectoryInfo(@"f:\temp\WalhallchogehaiKirerlibarlerho\");
            var git = new Git(directoryInfo);
            git.ExecuteCommand("add .");
            var (success, output) = git.ExecuteCommand("commit -m \"Format Code\"");
            if (success)
            {
                var branchName = $"t/bot/FormatCode_{DateTime.Now:yyMMddhhmmssfff}";

                git.CheckoutNewBranch(branchName);
                git.ExecuteCommand("push");

                var gitLabClient = new GitLabClient(options.GitlabUrl, options.GitlabToken);
                gitLabClient.MergeRequests.CreateAsync("",
                    new CreateMergeRequest(branchName, "dev", "[Bot] Automated PR to fix formatting errors"));
            }
            else
            {
                Console.WriteLine($"Do nothing.");
            }
        }
    }

    class Options
    {
        [Option("Gitlab")]
        public string GitlabUrl { set; get; }

        [Option("Token")]
        public string GitlabToken { set; get; }

        [Option("TargetBranch")]
        public string TargetBranch { set; get; }
    }

    static class GitHelper
    {
        public static (bool success, string output) ExecuteCommand(this Git git, string args)
        {
            return ProcessCommand.ExecuteCommand("git", args, git.Repo.FullName);
        }
    }
}
