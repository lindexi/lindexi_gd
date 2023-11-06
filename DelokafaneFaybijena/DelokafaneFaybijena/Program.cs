using System;
using System.IO;

namespace DelokafaneFaybijena
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var count = 0L;
            foreach (var directory in Directory.EnumerateDirectories("."))
            {
                Console.WriteLine($"[{count}][{DateTime.Now:yyyy-MM-dd hh:mm:ss}] {directory}");
                count++;
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
