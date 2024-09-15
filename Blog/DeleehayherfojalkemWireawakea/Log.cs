using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeleehayherfojalkemWireawakea;
internal static class Log
{
    public static void WriteLine(string? message = null)
    {
        Console.WriteLine(message);
        if (message != null)
        {
            var logFile = Path.Join(AppContext.BaseDirectory, "Log.txt");
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff}] {message}\r\n");
        }
    }
}
