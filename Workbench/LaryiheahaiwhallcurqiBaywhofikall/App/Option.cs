using System;
using System.Collections.Generic;
using System.Text;
using DotNetCampus.Cli.Compiler;

namespace App;


internal class Option
{
    [Option]
    public string? Foo { get; set; }
}
