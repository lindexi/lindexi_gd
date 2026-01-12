using DotNetCampus.Cli;
using DotNetCampus.Installer.Lib.Commandlines;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParserTest;

internal class StandardInstallerProgram
{
    /// <summary>
    /// 执行默认的命令行操作。比如调试或静默安装逻辑
    /// </summary>
    /// <returns></returns>
    public virtual async Task RunDefaultCommandLine(string[] args)
    {
        var commandLine = CommandLine.Parse(args);

        var result = await commandLine.AddHandler(async (DebugShowInstallerContentOption option) =>
        {
           
            return 0;
        })
            .RunAsync();

    }
}
