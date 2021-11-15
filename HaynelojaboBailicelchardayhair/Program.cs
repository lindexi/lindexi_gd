using System;
using System.IO;
using dotnetCampus.Cli;
using dotnetCampus.DotNETBuild.Utils;
using dotnetCampus.GitCommand;

namespace HaynelojaboBailicelchardayhair
{
    class Program
    {
        static void Main(string[] args)
        {
            var directoryInfo = new DirectoryInfo(@"f:\temp\WalhallchogehaiKirerlibarlerho\");
            var git = new Git(directoryInfo);
            git.ExecuteCommand("add .");
            var (success, output) = git.ExecuteCommand("commit -m \"Format Code\"");
            if (success)
            {
                git.CheckoutNewBranch($"t/bot/FormatCode_{DateTime.Now:yyMMddhhmmssfff}");
                git.ExecuteCommand("push");
            }
        }
    }

    class Options
    {
        [Option("Gitlab")]
        public string GitlabUrl { set; get; }

        [Option("Token")]
        public string GitlabToken { set; get; }
    }

    static class GitHelper
    {
        public static (bool success, string output) ExecuteCommand(this Git git, string args)
        {
            return ProcessCommand.ExecuteCommand("git", args, git.Repo.FullName);
        }
    }
}
