// See https://aka.ms/new-console-template for more information

using CommandLineParserTest;

<<<<<<< HEAD
//if (args.Length == 0)
//{
//    args =
//    [
//        "debug",
//        "ShowContent"
//    ];
//}
=======
args =
[
    "debug",
    "ShowContent"
];
>>>>>>> 98268e2fbab4bc2c01f987ec5b76e996cf23c6f9

var standardInstallerProgram = new StandardInstallerProgram();
await standardInstallerProgram.RunDefaultCommandLine(args);

Console.WriteLine("Hello, World!");