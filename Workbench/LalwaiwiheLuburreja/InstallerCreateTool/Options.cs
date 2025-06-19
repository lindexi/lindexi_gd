using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Cli.Compiler;

namespace InstallerCreateTool;

internal class Options
{
    [Option()]
    public required string PackingFolder { get; init; }

    [Option()]
    public string? InstallerOutputFolder { get; init; }

    [Option()]
    public required string InstallerBoostProjectPath { get; init; }
}
