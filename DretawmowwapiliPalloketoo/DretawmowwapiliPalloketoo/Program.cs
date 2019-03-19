using System;
using Microsoft.WindowsAPICodePack.Shell;

namespace DretawmowwapiliPalloketoo
{
    class Program
    {
        static void Main(string[] args)
        {
            NonFileSystemKnownFolder folder =(NonFileSystemKnownFolder)
                Microsoft.WindowsAPICodePack.Shell.KnownFolderHelper.FromPath(
                    "::{645FF040-5081-101B-9F08-00AA002F954E}");
            var desktop = (FileSystemKnownFolder) folder.Parent;
            //desktop.FolderId {b4bfcc3a-db2c-424c-b029-7fe99a87c641}

            Console.WriteLine(desktop.CanonicalName); // Desktop
             
            Console.WriteLine(folder);
         }
    }
}
