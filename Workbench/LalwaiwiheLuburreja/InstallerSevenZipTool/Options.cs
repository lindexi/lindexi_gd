using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Cli.Compiler;

namespace InstallerSevenZipTool;

internal class Options
{
    [Option('f', "InputDirectory")]
    public required string InputDirectory { get; init; }

    [Option('o', "OutputFile")]
    public required string OutputFile { get; init; }

    [Option("Ignore-Checksum")]
    public bool? IgnoreChecksum { get; init; }
}
