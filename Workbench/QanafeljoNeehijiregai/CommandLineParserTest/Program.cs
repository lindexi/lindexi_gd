// See https://aka.ms/new-console-template for more information
using CommandLineParserTest;

//if (args.Length == 0)
//{
//    args =
//    [
//        "debug",
//        "ShowContent"
//    ];
//}

var standardInstallerProgram = new StandardInstallerProgram();
await standardInstallerProgram.RunDefaultCommandLine(args);

Console.WriteLine("Hello, World!");