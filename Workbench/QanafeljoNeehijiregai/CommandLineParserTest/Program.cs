// See https://aka.ms/new-console-template for more information
using CommandLineParserTest;

var standardInstallerProgram = new StandardInstallerProgram();
await standardInstallerProgram.RunDefaultCommandLine(args);

Console.WriteLine("Hello, World!");