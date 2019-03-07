using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NelearrenouwowHalijicur
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = new DirectoryInfo(args[0]);

            var image = new DirectoryInfo(args[1]);

            DirectoryCopy(image.FullName, Path.Combine(folder.FullName, "image", image.Name));

            var str = new StringBuilder();

            str.Append("---\r\n");
            str.Append("title: ");
            str.Append(image.Name);
            str.Append("\r\n");
            str.Append("---\r\n");
            str.Append("\r\n");
            str.Append("<!--more-->");
            str.Append("\r\n");

            foreach (var temp in image.GetFiles()
                .Where(temp => temp.Extension == ".png" || temp.Extension == ".jpg" || temp.Extension == ".gif")
                .OrderBy(temp => temp.Name))
            {
                str.Append($"![](/image/{image.Name}/{temp.Name})\r\n");
            }

            var file = Path.Combine(folder.FullName, "_posts", image.Name + ".md");
            using (var stream = new StreamWriter(file))
            {
                stream.WriteLine(str.ToString());
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}