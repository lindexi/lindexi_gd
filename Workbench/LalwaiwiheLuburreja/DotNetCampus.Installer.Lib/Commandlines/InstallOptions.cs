using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Cli.Compiler;

namespace DotNetCampus.Installer.Lib.Commandlines;

[Verb(VerbName)]
public class InstallOptions
{
    public const string VerbName = "install";

    [Option(BoostPidOptionName)]
    public required string BoostPid { get; init; }

    public const string BoostPidOptionName = "BoostPid";

    [Option(SplashScreenWindowHandlerOptionName)]
    public long? SplashScreenWindowHandler { get; init; }

    public const string SplashScreenWindowHandlerOptionName = "SplashScreenWindowHandler";
}
