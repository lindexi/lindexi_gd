using System;

namespace LelyelqilairFalarleala
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Configuration.Abstractions/src/ConfigurationPath.cs
            Console.WriteLine(GetSectionKey("Foo:"));
        }

        public static string GetSectionKey(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var lastDelimiterIndex = path.LastIndexOf(KeyDelimiter, StringComparison.OrdinalIgnoreCase);
            return lastDelimiterIndex == -1 ? path : path.Substring(lastDelimiterIndex);
        }

        public static readonly string KeyDelimiter = ":";
    }
}
