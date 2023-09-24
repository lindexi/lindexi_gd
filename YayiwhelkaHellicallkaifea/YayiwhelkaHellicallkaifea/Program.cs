// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

var t = @"g:\Share\v\";
var s = args[0];

foreach (var directory in Directory.GetDirectories(s))
{
    var directoryInfo = new DirectoryInfo(directory);
    if (directoryInfo.CreationTime > DateTime.Now.AddDays(-2))
    {
        var folder = new DirectoryInfo(Path.Join(t, directoryInfo.Name));

        try
        {
            folder.CreateAsSymbolicLink(directoryInfo.FullName);
        }
        catch (System.IO.IOException e)
        {
            Console.WriteLine(e);
        }

        //var processStartInfo = new ProcessStartInfo("mklink")
        //{
        //    ArgumentList =
        //    {
        //        "/d",
        //        folder,
        //        directoryInfo.FullName
        //    }
        //};
        //Process.Start(processStartInfo);
    }
}